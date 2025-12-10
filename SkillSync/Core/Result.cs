namespace SkillSync.Core
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public List<string> Errors { get; }
        public T? Value { get; }

        protected Result(T? value, bool isSuccess, List<string>? errors = null)
        {
            Value = value;
            IsSuccess = isSuccess;
            Errors = errors ?? new List<string>();
        }

        public static Result<T> Success(T value)
            => new(value, true);

        public static Result<T> Failure(string error)
            => new(default, false, new List<string> { error });

        public static Result<T> Failure(List<string> errors)
            => new(default, false, errors);
    }

    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public List<string> Errors { get; }

        protected Result(bool isSuccess, List<string>? errors = null)
        {
            IsSuccess = isSuccess;
            Errors = errors ?? new List<string>();
        }

        public static Result Success() => new(true);
        public static Result Failure(string error) => new(false, new List<string> { error });
        public static Result Failure(List<string> errors) => new(false, errors);
    }
}