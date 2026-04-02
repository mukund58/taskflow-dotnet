public interface IProjectService
{
    Task<List<Project>> GetAll();
    Task<Project> Create(ProjectDto dto);
}
