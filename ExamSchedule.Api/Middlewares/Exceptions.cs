namespace ExamSchedule.Api.Middlewares;

public class BusinessException : Exception
{
    public BusinessException(string msg) : base(msg) { }
}

public class ConflictException : Exception
{
    public ConflictException(string msg) : base(msg) { }
}

public class NotFoundException : Exception
{
    public NotFoundException(string msg) : base(msg) { }
}