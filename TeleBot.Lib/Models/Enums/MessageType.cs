﻿using System.Text.Json.Serialization;

namespace TeleBot.Lib.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<MessageType>))]
public enum MessageType
{
    Unknown = 0,
    Text,
    Photo,
    Audio,
    Video,
    Voice,
    Document,
    Sticker,
    Location,
    Contact,
    Venue,
    Game,
    VideoNote,
    Invoice,
    SuccessfulPayment,
    WebsiteConnected,
    ChatMembersAdded,
    ChatMemberLeft,
    ChatTitleChanged,
    ChatPhotoChanged,
    MessagePinned,
    ChatPhotoDeleted,
    GroupCreated,
    SupergroupCreated,
    ChannelCreated,
    MigratedToSupergroup,
    MigratedFromGroup,
    Poll,
    Dice,
    MessageAutoDeleteTimerChanged,
    ProximityAlertTriggered,
    WebAppData,
    VideoChatScheduled,
    VideoChatStarted,
    VideoChatEnded,
    VideoChatParticipantsInvited,
    Animation,
    ForumTopicCreated,
    ForumTopicClosed,
    ForumTopicReopened,
    ForumTopicEdited,
    GeneralForumTopicHidden,
    GeneralForumTopicUnhidden,
    WriteAccessAllowed,
    UserShared,
    ChatShared,
}
