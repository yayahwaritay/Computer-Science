using CompSci.Core.DTOs;

namespace CompSci.Core.Validators;

public static class ValidationResult
{
    public static List<string> IsValid { get; } = new();
}

public static class AuthValidator
{
    public static List<string> ValidateRegister(RegisterRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Username))
            errors.Add("Username is required.");
        else if (request.Username.Length < 3 || request.Username.Length > 50)
            errors.Add("Username must be between 3 and 50 characters.");

        if (string.IsNullOrWhiteSpace(request.Email))
            errors.Add("Email is required.");
        else if (!IsValidEmail(request.Email))
            errors.Add("Invalid email format.");

        if (string.IsNullOrWhiteSpace(request.Password))
            errors.Add("Password is required.");
        else if (request.Password.Length < 8)
            errors.Add("Password must be at least 8 characters.");
        else if (!request.Password.Any(char.IsUpper) || !request.Password.Any(char.IsLower) || !request.Password.Any(char.IsDigit))
            errors.Add("Password must contain at least one uppercase letter, one lowercase letter, and one digit.");

        return errors;
    }

    public static List<string> ValidateLogin(LoginRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Email))
            errors.Add("Email is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            errors.Add("Password is required.");

        return errors;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

public static class CourseValidator
{
    public static List<string> Validate(CourseRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.CourseCode))
            errors.Add("Course code is required.");
        else if (request.CourseCode.Length > 20)
            errors.Add("Course code must not exceed 20 characters.");

        if (string.IsNullOrWhiteSpace(request.CourseName))
            errors.Add("Course name is required.");
        else if (request.CourseName.Length > 200)
            errors.Add("Course name must not exceed 200 characters.");

        if (request.CreditHour <= 0)
            errors.Add("Credit hour must be greater than 0.");
        else if (request.CreditHour > 10)
            errors.Add("Credit hour must not exceed 10.");

        if (string.IsNullOrWhiteSpace(request.Staff))
            errors.Add("Staff name is required.");

        return errors;
    }
}

public static class AssignmentValidator
{
    public static List<string> Validate(AssignmentRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.CourseName))
            errors.Add("Course name is required.");

        if (string.IsNullOrWhiteSpace(request.CourseCode))
            errors.Add("Course code is required.");

        if (string.IsNullOrWhiteSpace(request.AssignmentTitle))
            errors.Add("Assignment title is required.");
        else if (request.AssignmentTitle.Length > 300)
            errors.Add("Assignment title must not exceed 300 characters.");

        if (request.DueDate == default)
            errors.Add("Due date is required.");
        else if (request.DueDate < DateTime.UtcNow.Date)
            errors.Add("Due date cannot be in the past.");

        return errors;
    }
}

public static class StudentValidator
{
    public static List<string> Validate(StudentRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.FirstName))
            errors.Add("First name is required.");
        else if (request.FirstName.Length > 100)
            errors.Add("First name must not exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(request.LastName))
            errors.Add("Last name is required.");
        else if (request.LastName.Length > 100)
            errors.Add("Last name must not exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(request.StudentId))
            errors.Add("Student ID is required.");
        else if (request.StudentId.Length > 20)
            errors.Add("Student ID must not exceed 20 characters.");

        if (string.IsNullOrWhiteSpace(request.ProgramName))
            errors.Add("Program name is required.");

        if (request.Year <= 0)
            errors.Add("Year must be greater than 0.");
        else if (request.Year > 10)
            errors.Add("Year must not exceed 10.");

        if (request.EnrollmentYear < 1900 || request.EnrollmentYear > DateTime.UtcNow.Year + 1)
            errors.Add("Invalid enrollment year.");

        if (request.ExpectedGraduation < request.EnrollmentYear)
            errors.Add("Expected graduation year must be after enrollment year.");

        return errors;
    }
}

public static class PastQuestionValidator
{
    public static List<string> Validate(PastQuestionRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.CourseName))
            errors.Add("Course name is required.");

        if (string.IsNullOrWhiteSpace(request.CourseCode))
            errors.Add("Course code is required.");

        return errors;
    }
}

public static class NoteValidator
{
    public static List<string> Validate(NoteRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.CourseName))
            errors.Add("Course name is required.");

        if (string.IsNullOrWhiteSpace(request.CourseCode))
            errors.Add("Course code is required.");

        return errors;
    }
}
