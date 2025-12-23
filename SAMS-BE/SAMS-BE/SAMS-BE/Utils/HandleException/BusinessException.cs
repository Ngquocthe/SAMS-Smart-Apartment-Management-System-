namespace SAMS_BE.Utils.HanldeException
{
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message) { }
    }
}
