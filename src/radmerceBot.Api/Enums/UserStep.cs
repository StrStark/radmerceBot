namespace radmerceBot.Api.Enums;

public enum UserStep
{
    Start = 0,
    WaitingForFullName = 1,
    WaitingForPhone = 2,
    WaitingForOtp = 3,
    Registered = 4,
    OfferedPaidCourse = 5,
    RequestedConsultation = 6
}
