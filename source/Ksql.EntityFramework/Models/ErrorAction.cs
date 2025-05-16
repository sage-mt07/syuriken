namespace Ksql.EntityFramework.Models;


public enum ErrorAction
{
    Stop,

    Skip,

    LogAndContinue,

    DeadLetterQueue
}
