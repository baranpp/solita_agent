namespace SolitaAgent.Api.Validators;

public interface IInputSanitizer
{
    string Sanitize(string? input);
}
