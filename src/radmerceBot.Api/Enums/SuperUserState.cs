namespace radmerceBot.Api.Enums;

public enum SuperUserState
{
    None,
    SendingMessageToUser,
    SendingSmsToUser,
    Dashboard,
    ExportingCsv,
    SearchingContacts,
    SendingSms,
    SendingSms_Menu,
    SendingSms_Single_WaitingForPhone,
    SendingSms_Single_WaitingForMessage,
    SendingSms_Bulk_WaitingForFile,
    ManagingVideos,                
    AddingVideo_WaitingForFile,
    AddingVideo_WaitingForCaption,
    SendingMessageToActiveUsers
}
