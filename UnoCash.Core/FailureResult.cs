namespace UnoCash.Core
{
    class FailureResult<T> : IResult<T>
    {
        internal readonly string Failure;

        internal FailureResult(string failure) => 
            Failure = failure;
    }
}