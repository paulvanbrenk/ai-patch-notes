using NanoidDotNet;

namespace PatchNotes.Data;

public static class IdGenerator
{
    public static string NewId() => Nanoid.Generate(size: 21);
}
