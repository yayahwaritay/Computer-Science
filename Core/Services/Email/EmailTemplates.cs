namespace CompSci.Core.Services.Email;

public static class EmailTemplates
{
    public static (string Subject, string Html) StudentWelcome(string fullName, string email, string tempPassword)
    {
        const string subject = "Welcome to CompSci Portal - Your Account Details";
        var html = $"""
            <p>Hi {fullName},</p>
            <p>An account has been created for you on the CompSci Portal by an administrator or lecturer.</p>
            <p><strong>Email:</strong> {email}<br/>
            <strong>Temporary password:</strong> {tempPassword}</p>
            <p>Please log in and change your password as soon as possible.</p>
            <p>Regards,<br/>CompSci Team</p>
            """;
        return (subject, html);
    }

    public static (string Subject, string Html) RegistrationReceived(string fullName)
    {
        const string subject = "CompSci Portal Registration Received";
        var html = $"""
            <p>Hi {fullName},</p>
            <p>Thanks for registering on the CompSci Portal. Your registration has been received and is
            <strong>pending approval</strong> by an administrator or lecturer.</p>
            <p>You'll receive another email once your account has been approved and you can log in.</p>
            <p>Regards,<br/>CompSci Team</p>
            """;
        return (subject, html);
    }

    public static (string Subject, string Html) RegistrationApproved(string fullName)
    {
        const string subject = "Your CompSci Portal Registration Has Been Approved";
        var html = $"""
            <p>Hi {fullName},</p>
            <p>Good news - your registration has been approved. You can now log in to the CompSci Portal
            with the email and password you registered with.</p>
            <p>Regards,<br/>CompSci Team</p>
            """;
        return (subject, html);
    }

    public static (string Subject, string Html) RegistrationRejected(string fullName)
    {
        const string subject = "CompSci Portal Registration Update";
        var html = $"""
            <p>Hi {fullName},</p>
            <p>Unfortunately, your registration on the CompSci Portal was not approved. If you believe this
            is a mistake, please contact an administrator or lecturer.</p>
            <p>Regards,<br/>CompSci Team</p>
            """;
        return (subject, html);
    }
}
