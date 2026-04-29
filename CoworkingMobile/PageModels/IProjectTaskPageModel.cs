using CommunityToolkit.Mvvm.Input;
using CoworkingMobile.Models;

namespace CoworkingMobile.PageModels;

public interface IProjectTaskPageModel
{
    IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
    bool IsBusy { get; }
}