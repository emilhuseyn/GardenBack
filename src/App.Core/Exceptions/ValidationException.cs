namespace App.Core.Exceptions
{
    public class ValidationException : Exception
    {
        public IEnumerable<string> Errors { get; }

        public ValidationException(string message) : base(message)
        {
            Errors = new[] { message };
        }

        public ValidationException(IEnumerable<string> errors) : base("Bir və ya daha çox doğrulama xətası baş verdi.")
        {
            Errors = errors;
        }
    }
}
