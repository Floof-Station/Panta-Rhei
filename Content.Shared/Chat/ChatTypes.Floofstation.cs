using Robust.Shared.Serialization;

namespace Content.Shared.Chat;

// Floofstation: InGameICChatType and InGameOOCChatType have been moved to shared due to integration with the language system
// ReSharper disable InconsistentNaming

/// <summary>
///     InGame IC chat is for chat that is specifically ingame (not lobby) but is also in character, i.e. speaking.
/// </summary>
[Serializable, NetSerializable]
public enum InGameICChatType : byte
{
    Speak,
    Emote,
    Whisper,
    Telepathic, //Nyano - Summary: adds telepathic as a type of message users can receive.
    Subtle, // Floofstation
}

/// <summary>
///     InGame OOC chat is for chat that is specifically ingame (not lobby) but is OOC, like deadchat or LOOC.
/// </summary>
[Serializable, NetSerializable]
public enum InGameOOCChatType : byte
{
    Looc,
    Dead,
    SubtleLOOC,  // Floofstation - unlike pre-rebase, this is an OOC channel
}
