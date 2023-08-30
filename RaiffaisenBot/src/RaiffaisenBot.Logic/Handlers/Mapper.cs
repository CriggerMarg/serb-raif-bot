using Telegram.Bot.Types.Enums;

namespace RaiffaisenBot.Logic.Handlers;

public static class Mapper
{
    public static HandlerMessageType MapToHandlerMessageType(MessageType messageType)
    {
        return messageType switch
        {
            MessageType.Unknown => HandlerMessageType.Unknown,
            MessageType.Text => HandlerMessageType.Text,
            MessageType.Photo => HandlerMessageType.Photo,
            MessageType.Audio => HandlerMessageType.Audio,
            MessageType.Video => HandlerMessageType.Video,
            MessageType.Voice => HandlerMessageType.Voice,
            MessageType.Document => HandlerMessageType.Document,
            MessageType.Sticker => HandlerMessageType.Sticker,
            MessageType.Location => HandlerMessageType.Location,
            MessageType.Contact => HandlerMessageType.Contact,
            MessageType.Venue => HandlerMessageType.Venue,
            MessageType.Game => HandlerMessageType.Game,
            MessageType.VideoNote => HandlerMessageType.VideoNote,
            MessageType.Invoice => HandlerMessageType.Invoice,
            MessageType.SuccessfulPayment => HandlerMessageType.SuccessfulPayment,
            MessageType.WebsiteConnected => HandlerMessageType.WebsiteConnected,
            MessageType.ChatMembersAdded => HandlerMessageType.ChatMembersAdded,
            MessageType.ChatMemberLeft => HandlerMessageType.ChatMemberLeft,
            MessageType.ChatTitleChanged => HandlerMessageType.ChatTitleChanged,
            MessageType.ChatPhotoChanged => HandlerMessageType.ChatPhotoChanged,
            MessageType.MessagePinned => HandlerMessageType.MessagePinned,
            MessageType.ChatPhotoDeleted => HandlerMessageType.ChatPhotoDeleted,
            MessageType.GroupCreated => HandlerMessageType.GroupCreated,
            MessageType.SupergroupCreated => HandlerMessageType.SupergroupCreated,
            MessageType.ChannelCreated => HandlerMessageType.ChannelCreated,
            MessageType.MigratedToSupergroup => HandlerMessageType.MigratedToSupergroup,
            MessageType.MigratedFromGroup => HandlerMessageType.MigratedFromGroup,
            MessageType.Poll => HandlerMessageType.Poll,
            MessageType.Dice => HandlerMessageType.Dice,
            MessageType.MessageAutoDeleteTimerChanged => HandlerMessageType.MessageAutoDeleteTimerChanged,
            MessageType.ProximityAlertTriggered => HandlerMessageType.ProximityAlertTriggered,
            MessageType.WebAppData => HandlerMessageType.WebAppData,
            MessageType.VideoChatScheduled => HandlerMessageType.VideoChatScheduled,
            MessageType.VideoChatStarted => HandlerMessageType.VideoChatStarted,
            MessageType.VideoChatEnded => HandlerMessageType.VideoChatEnded,
            MessageType.VideoChatParticipantsInvited => HandlerMessageType.VideoChatParticipantsInvited,
            MessageType.Animation => HandlerMessageType.Animation,
            MessageType.ForumTopicCreated => HandlerMessageType.ForumTopicCreated,
            MessageType.ForumTopicClosed => HandlerMessageType.ForumTopicClosed,
            MessageType.ForumTopicReopened => HandlerMessageType.ForumTopicReopened,
            MessageType.ForumTopicEdited => HandlerMessageType.ForumTopicEdited,
            MessageType.GeneralForumTopicHidden => HandlerMessageType.GeneralForumTopicHidden,
            MessageType.GeneralForumTopicUnhidden => HandlerMessageType.GeneralForumTopicUnhidden,
            MessageType.WriteAccessAllowed => HandlerMessageType.WriteAccessAllowed,
            _ => throw new ArgumentException($"Unsupported MessageType: {messageType}")
        };
    }
}
