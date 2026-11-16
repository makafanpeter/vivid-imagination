using CommunityToolkit.Mvvm.Input;
using VividImagination.Mobile.Models;

namespace VividImagination.Mobile.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}