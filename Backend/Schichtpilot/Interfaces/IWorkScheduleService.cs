namespace Schichtpilot.Interfaces;

public interface IWorkScheduleService
{   
    Task GenerateSchedule();

    Task ReGenerateSchedule();

    Task PublishSchedule();
    
    Task ViewSchedules();
    
    Task ViewSchedule();

    Task DeleteSchedule();
    
    Task SetScheduleActive();
    
    Task SetScheduleOffline();

    Task CloneSchedule();

    Task ChangeScheduleDate();
}