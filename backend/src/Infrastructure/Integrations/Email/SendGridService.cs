using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OnlineCommunities.Core.Entities.Notifications;
using OnlineCommunities.Core.Interfaces;

namespace OnlineCommunities.Infrastructure.Integrations.Email;

/// <summary>
/// SendGrid email service implementation for sending email notifications.
/// Provides integration with SendGrid API for reliable email delivery.
/// </summary>
public class SendGridService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<SendGridService> _logger;

    /// <summary>
    /// Initializes a new instance of the SendGridService.
    /// </summary>
    /// <param name="options">Email configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public SendGridService(IOptions<EmailOptions> options, ILogger<SendGridService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> SendEmailAsync(
        string to, 
        string subject, 
        string htmlContent, 
        string? textContent = null, 
        string? fromAddress = null, 
        string? fromName = null,
        Dictionary<string, object>? templateData = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending email to {To} with subject {Subject}", to, subject);

            // TODO: Implement actual SendGrid API integration
            // This is a stub implementation that simulates email sending
            await Task.Delay(100, cancellationToken); // Simulate API call

            // Validate email address format
            if (!IsValidEmailAddress(to))
            {
                _logger.LogWarning("Invalid email address format: {To}", to);
                return false;
            }

            // TODO: Replace with actual SendGrid API call
            // Example implementation would use SendGrid C# SDK:
            // var client = new SendGridClient(_options.ApiKey);
            // var msg = new SendGridMessage()
            // {
            //     From = new EmailAddress(fromAddress ?? _options.FromAddress, fromName ?? _options.FromName),
            //     Subject = subject,
            //     PlainTextContent = textContent,
            //     HtmlContent = htmlContent
            // };
            // msg.AddTo(new EmailAddress(to));
            // var response = await client.SendEmailAsync(msg, cancellationToken);

            _logger.LogInformation("Successfully sent email to {To}", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendTemplateEmailAsync(
        string to, 
        string templateId, 
        Dictionary<string, object> templateData, 
        string? fromAddress = null, 
        string? fromName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending template email to {To} using template {TemplateId}", to, templateId);

            // TODO: Implement actual SendGrid template API integration
            // This is a stub implementation that simulates template email sending
            await Task.Delay(100, cancellationToken); // Simulate API call

            // Validate email address format
            if (!IsValidEmailAddress(to))
            {
                _logger.LogWarning("Invalid email address format: {To}", to);
                return false;
            }

            // TODO: Replace with actual SendGrid template API call
            // Example implementation would use SendGrid C# SDK:
            // var client = new SendGridClient(_options.ApiKey);
            // var msg = new SendGridMessage()
            // {
            //     From = new EmailAddress(fromAddress ?? _options.FromAddress, fromName ?? _options.FromName),
            //     TemplateId = templateId
            // };
            // msg.AddTo(new EmailAddress(to));
            // msg.SetTemplateData(templateData);
            // var response = await client.SendEmailAsync(msg, cancellationToken);

            _logger.LogInformation("Successfully sent template email to {To} using template {TemplateId}", to, templateId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send template email to {To} using template {TemplateId}", to, templateId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendBulkEmailAsync(
        IEnumerable<string> recipients, 
        string subject, 
        string htmlContent, 
        string? textContent = null, 
        string? fromAddress = null, 
        string? fromName = null,
        Dictionary<string, object>? templateData = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var recipientList = recipients.ToList();
            _logger.LogInformation("Sending bulk email to {RecipientCount} recipients", recipientList.Count);

            // TODO: Implement actual SendGrid bulk email API integration
            // This is a stub implementation that simulates bulk email sending
            await Task.Delay(200, cancellationToken); // Simulate API call

            // Validate all email addresses
            var invalidEmails = recipientList.Where(email => !IsValidEmailAddress(email)).ToList();
            if (invalidEmails.Any())
            {
                _logger.LogWarning("Invalid email addresses found: {InvalidEmails}", string.Join(", ", invalidEmails));
                return false;
            }

            // TODO: Replace with actual SendGrid bulk email API call
            // This would typically use SendGrid's batch processing capabilities

            _logger.LogInformation("Successfully sent bulk email to {RecipientCount} recipients", recipientList.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk email to {RecipientCount} recipients", recipients.Count());
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateEmailAddressAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating email address: {Email}", email);

            // TODO: Implement actual SendGrid email validation API
            // This is a stub implementation that uses basic regex validation
            await Task.Delay(10, cancellationToken); // Simulate API call

            var isValid = IsValidEmailAddress(email);
            
            _logger.LogDebug("Email validation result for {Email}: {IsValid}", email, isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate email address: {Email}", email);
            return false;
        }
    }

    /// <summary>
    /// Validates email address format using basic regex pattern.
    /// </summary>
    /// <param name="email">The email address to validate.</param>
    /// <returns>True if the email format is valid, false otherwise.</returns>
    private static bool IsValidEmailAddress(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

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

/// <summary>
/// Interface for email service operations.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email with HTML and/or text content.
    /// </summary>
    /// <param name="to">The recipient's email address.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="htmlContent">The HTML content of the email.</param>
    /// <param name="textContent">The plain text content of the email.</param>
    /// <param name="fromAddress">The sender's email address.</param>
    /// <param name="fromName">The sender's display name.</param>
    /// <param name="templateData">Additional template data for personalization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the email was sent successfully, false otherwise.</returns>
    Task<bool> SendEmailAsync(
        string to, 
        string subject, 
        string htmlContent, 
        string? textContent = null, 
        string? fromAddress = null, 
        string? fromName = null,
        Dictionary<string, object>? templateData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email using a SendGrid template.
    /// </summary>
    /// <param name="to">The recipient's email address.</param>
    /// <param name="templateId">The SendGrid template ID.</param>
    /// <param name="templateData">The data to substitute in the template.</param>
    /// <param name="fromAddress">The sender's email address.</param>
    /// <param name="fromName">The sender's display name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the email was sent successfully, false otherwise.</returns>
    Task<bool> SendTemplateEmailAsync(
        string to, 
        string templateId, 
        Dictionary<string, object> templateData, 
        string? fromAddress = null, 
        string? fromName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends bulk emails to multiple recipients.
    /// </summary>
    /// <param name="recipients">The list of recipient email addresses.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="htmlContent">The HTML content of the email.</param>
    /// <param name="textContent">The plain text content of the email.</param>
    /// <param name="fromAddress">The sender's email address.</param>
    /// <param name="fromName">The sender's display name.</param>
    /// <param name="templateData">Additional template data for personalization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if all emails were sent successfully, false otherwise.</returns>
    Task<bool> SendBulkEmailAsync(
        IEnumerable<string> recipients, 
        string subject, 
        string htmlContent, 
        string? textContent = null, 
        string? fromAddress = null, 
        string? fromName = null,
        Dictionary<string, object>? templateData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an email address using SendGrid's validation service.
    /// </summary>
    /// <param name="email">The email address to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the email is valid, false otherwise.</returns>
    Task<bool> ValidateEmailAddressAsync(string email, CancellationToken cancellationToken = default);
}
