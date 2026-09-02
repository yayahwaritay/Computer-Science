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

    public static (string Subject, string Html) OrganizationCredentialsIssued(string organizationName, string email, string password, DateTime expiresAt)
    {
        const string subject = "CompSci Portal - Internship Evaluation Access";
        var html = $"""
            <p>Hi {organizationName},</p>
            <p>An account has been created for you on the CompSci Portal so you can submit internship
            evaluations for the students you hosted.</p>
            <p><strong>Email:</strong> {email}<br/>
            <strong>Password:</strong> {password}</p>
            <p>This password expires on <strong>{expiresAt:d MMMM yyyy}</strong>. Please log in and change
            it before then - if it expires, an administrator or lecturer will need to reissue your
            credentials.</p>
            <p>Regards,<br/>CompSci Team</p>
            """;
        return (subject, html);
    }

    public static (string Subject, string Html) SubmissionUploaded(string lecturerName, string studentFullName, string submissionTypeLabel)
    {
        var subject = $"{studentFullName} submitted their {submissionTypeLabel}";
        var html = $"""
            <p>Hi {lecturerName},</p>
            <p><strong>{studentFullName}</strong>, a student allocated to you, has just uploaded (or re-uploaded)
            their <strong>{submissionTypeLabel}</strong> on the CompSci Portal.</p>
            <p>Log in to review it and leave a comment when you're ready.</p>
            <p>Regards,<br/>CompSci Team</p>
            """;
        return (subject, html);
    }

    public static (string Subject, string Html) SubmissionCommented(string studentFullName, string submissionTypeLabel)
    {
        var subject = $"New comment on your {submissionTypeLabel}";
        var html = $"""
            <p>Hi {studentFullName},</p>
            <p>Your lecturer/administrator has left a new comment on your <strong>{submissionTypeLabel}</strong>
            submission on the CompSci Portal.</p>
            <p>Log in to view it.</p>
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
