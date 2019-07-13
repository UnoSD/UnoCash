namespace UnoCash.Core
{
    class SuccessResult<T> : IResult<T>
    {
        internal readonly T Success;

        public SuccessResult(T success) => 
            Success = success;
    }
}