using Microsoft.Extensions.Logging;
using OnlineCommunities.Application.Common.CQRS;
using OnlineCommunities.Infrastructure.Governance;

namespace OnlineCommunities.Application.Commands.Governance;

/// <summary>
/// Handler for rotating certificates.
/// Provides certificate rotation with validation and error handling.
/// </summary>
public class RotateCertificateCommandHandler : ICommandHandler<RotateCertificateCommand, bool>
{
    private readonly CertificateRotationService _certificateRotationService;
    private readonly ILogger<RotateCertificateCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the RotateCertificateCommandHandler.
    /// </summary>
    /// <param name="certificateRotationService">The certificate rotation service.</param>
    /// <param name="logger">Logger instance.</param>
    public RotateCertificateCommandHandler(
        CertificateRotationService certificateRotationService,
        ILogger<RotateCertificateCommandHandler> logger)
    {
        _certificateRotationService = certificateRotationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> HandleAsync(RotateCertificateCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing certificate rotation command for resource {ResourceId}, certificate {CertificateName}",
                command.ResourceId, command.CertificateName);

            // Validate command parameters
            if (string.IsNullOrWhiteSpace(command.ResourceId))
            {
                throw new ArgumentException("Resource ID cannot be null or empty", nameof(command.ResourceId));
            }

            if (string.IsNullOrWhiteSpace(command.CertificateName))
            {
                throw new ArgumentException("Certificate name cannot be null or empty", nameof(command.CertificateName));
            }

            // Check if rotation is needed (unless forced)
            if (!command.ForceRotation)
            {
                var validationResult = await _certificateRotationService.ValidateCertificateAsync(
                    command.ResourceId,
                    command.CertificateName,
                    cancellationToken);

                if (!validationResult.NeedsRotation)
                {
                    _logger.LogInformation(
                        "Certificate {CertificateName} for resource {ResourceId} does not need rotation",
                        command.CertificateName, command.ResourceId);
                    return true; // No rotation needed, consider it successful
                }
            }

            // Perform the certificate rotation
            var rotationSuccess = await _certificateRotationService.RotateCertificateAsync(
                command.ResourceId,
                command.CertificateName,
                cancellationToken);

            if (!rotationSuccess)
            {
                _logger.LogError(
                    "Certificate rotation failed for resource {ResourceId}, certificate {CertificateName}",
                    command.ResourceId, command.CertificateName);
                return false;
            }

            // Validate the certificate after rotation if requested
            if (command.ValidateAfterRotation)
            {
                _logger.LogInformation(
                    "Validating certificate {CertificateName} after rotation",
                    command.CertificateName);

                var postRotationValidation = await _certificateRotationService.ValidateCertificateAsync(
                    command.ResourceId,
                    command.CertificateName,
                    cancellationToken);

                if (!postRotationValidation.IsValid)
                {
                    _logger.LogError(
                        "Certificate validation failed after rotation for resource {ResourceId}, certificate {CertificateName}. Errors: {Errors}",
                        command.ResourceId, command.CertificateName, string.Join(", ", postRotationValidation.Errors));
                    return false;
                }

                _logger.LogInformation(
                    "Certificate validation successful after rotation for resource {ResourceId}, certificate {CertificateName}",
                    command.ResourceId, command.CertificateName);
            }

            _logger.LogInformation(
                "Successfully processed certificate rotation command for resource {ResourceId}, certificate {CertificateName}",
                command.ResourceId, command.CertificateName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process certificate rotation command for resource {ResourceId}, certificate {CertificateName}",
                command.ResourceId, command.CertificateName);
            throw;
        }
    }
}
