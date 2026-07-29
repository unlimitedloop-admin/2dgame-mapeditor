using StageSmith.Core.Models;

namespace StageSmith.Application.Services;

public interface IConfigRepository
{
    EditorConfig Load();
    void Save(EditorConfig config);
}
