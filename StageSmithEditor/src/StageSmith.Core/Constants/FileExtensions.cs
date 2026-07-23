namespace StageSmith.Core.Constants;

public static class FileExtensions
{
    public const string Project = ".sseproj";
    public const string StageFile = ".ssestage";

    public const string StageMapBinary = ".bin";
    public const string StageDefinition = ".def";

    public const string ProjectFilter =
        "StageSmith Project (*.sseproj)|*.sseproj|All files (*.*)|*.*";

    public const string BinaryFilter =
        "Stage Map Binary (*.bin)|*.bin|All files (*.*)|*.*";

    public const string ImageFilter =
        "Image Files (*.png;*.bmp)|*.png;*.bmp|All files (*.*)|*.*";

    public const string StageFileFilter =
        "StageSmith Stage (*.ssestage)|*.ssestage|All files (*.*)|*.*";
}
