using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using ReactiveUI;
using WalletWasabi.Fluent.ViewModels.Wallets.Buy.Messages;
using WalletWasabi.Fluent.ViewModels.Wallets.Buy.Workflows;

namespace WalletWasabi.Fluent.ViewModels.Wallets.Buy;

public partial class OrderViewModel : ReactiveObject
{
	private readonly ReadOnlyObservableCollection<MessageViewModel> _messages;
	private readonly SourceList<MessageViewModel> _messagesList;
	private readonly IWorkflowManager _workflowManager;
	private readonly IOrderManager _orderManager;

	[AutoNotify] private bool _isBusy;
	[AutoNotify] private bool _isCompleted;
	[AutoNotify] private bool _hasUnreadMessages;
	[AutoNotify] private MessageViewModel? _selectedMessage;

	public OrderViewModel(
		Guid id,
		string title,
		IWorkflowManager workflowManager,
		IOrderManager orderManager)
	{
		Id = id;
		Title = title;

		// TODO: For now we have only one workflow manager.
		_workflowManager = workflowManager;
		_orderManager = orderManager;

		_messagesList = new SourceList<MessageViewModel>();

		_messagesList
			.Connect()
			.Bind(out _messages)
			.Subscribe();

		_workflowManager.SelectNextWorkflow();

		SendCommand = ReactiveCommand.CreateFromTask(SendAsync, _workflowManager.WorkflowValidator.IsValidObservable);

		RemoveOrderCommand = ReactiveCommand.Create(RemoveOrder);

		_orderManager.UpdateTrigger.Subscribe(_=> UpdateOrder());

		UpdateOrder();

		// TODO: Run initial workflow steps if any.
		// RunNoInputWorkflowSteps();
	}

	public Guid Id { get; }

	public string Title { get; }

	public IReadOnlyCollection<MessageViewModel> Messages => _messages;

	public IWorkflowManager WorkflowManager => _workflowManager;

	public ICommand SendCommand { get; }

	public ICommand RemoveOrderCommand { get;  }

	private void UpdateOrder()
	{
		IsCompleted = _orderManager.IsCompleted(Id);
		HasUnreadMessages = _orderManager.HasUnreadMessages(Id);
		// TODO: Update messages etc.
	}

	private async Task SendAsync()
	{
		IsBusy = true;

		try
		{
			_workflowManager.WorkflowValidator.Signal(false);

			// TODO: Only for form messages and not api calls.
			await Task.Delay(500);

			if (_workflowManager.CurrentWorkflow?.CurrentStep is null)
			{
				return;
			}

			var message = _workflowManager.CurrentWorkflow.CurrentStep.UserInputValidator.GetFinalMessage();
			if (message is null)
			{
				return;
			}

			AddUserMessage(message);

			var nextStep = _workflowManager.CurrentWorkflow.ExecuteNextStep();
			if (nextStep is null)
			{
				// TODO: Handle error?
				return;
			}

			if (!nextStep.RequiresUserInput)
			{
				var nextMessage = nextStep.UserInputValidator.GetFinalMessage();
				if (nextMessage is not null)
				{
					AddAssistantMessage(nextMessage);
				}
			}

			if (nextStep.IsCompleted)
			{
				RunNoInputWorkflowSteps();
			}

			if (_workflowManager.CurrentWorkflow.IsCompleted)
			{
				// TODO: Send request to api service.
				await _workflowManager.SendApiRequestAsync();

				// TODO: Select next workflow or wait for api service response.
				_workflowManager.SelectNextWorkflow();

				_workflowManager.WorkflowValidator.Signal(false);

				// TODO: After workflow is completed we either wait for service api message or check if next workflow can be run.
				RunNoInputWorkflowSteps();
			}
		}
		catch (Exception exception)
		{
			// TODO: Add propert error handling.
			AddErrorMessage($"Error: {exception.Message}");
		}
		finally
		{
			IsBusy = false;
		}
	}

	private void RunNoInputWorkflowSteps()
	{
		if (_workflowManager.CurrentWorkflow is null)
		{
			return;
		}

		while (true)
		{
			var peekStep = _workflowManager.CurrentWorkflow.PeekNextStep();
			if (peekStep is null)
			{
				break;
			}

			var nextStep = _workflowManager.CurrentWorkflow.ExecuteNextStep();
			if (nextStep is not null)
			{
				if (nextStep.UserInputValidator.Message is not null)
				{
					AddAssistantMessage(nextStep.UserInputValidator.Message);
				}

				if (nextStep.RequiresUserInput)
				{
					break;
				}
			}
		}
	}

	private void AddErrorMessage(string message)
	{
		var assistantMessage = new ErrorMessageViewModel
		{
			Message = message
		};

		_messagesList.Edit(x =>
		{
			x.Add(assistantMessage);
		});

		SelectedMessage = assistantMessage;
	}

	private void AddAssistantMessage(string message)
	{
		var assistantMessage = new AssistantMessageViewModel
		{
			Message = message
		};

		_messagesList.Edit(x =>
		{
			x.Add(assistantMessage);
		});

		SelectedMessage = assistantMessage;
	}

	private void AddUserMessage(string message)
	{
		var userMessage = new UserMessageViewModel
		{
			Message = message
		};

		_messagesList.Edit(x =>
		{
			x.Add(userMessage);
		});

		SelectedMessage = userMessage;
	}

	private void RemoveOrder()
	{
		_orderManager.RemoveOrder(Id);
	}

	public void Update()
	{
		// TODO: For testing
		RunNoInputWorkflowSteps();
	}
}
