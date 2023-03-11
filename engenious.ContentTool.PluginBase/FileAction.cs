namespace engenious.ContentTool;

[Flags]
public enum FileAction
{
    Ask = 1,
    Overwrite = 2,
    Repeat = 4,
    Skip = 8
}