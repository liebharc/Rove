namespace Rove.Model
{
    public class Result
    {
        public enum Summary
        {
            Error,
            Sucess
        }

        public static Result Error(string message)
        {
            return new Result(Summary.Error, message);
        }

        public static Result Success { get; } = new Result(Summary.Sucess, string.Empty);

        public Result(Summary summary, string message)
        {
            IsError = summary == Summary.Error;
            IsOk = summary == Summary.Sucess;
            Message = message;
        }

        public bool IsError { get; }
        public bool IsOk { get; }
        public string Message { get; }
    }
}
