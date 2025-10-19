using OnlineCommunities.Application.Common.CQRS;

namespace OnlineCommunities.Application.Commands.Governance;

/// <summary>
/// Command to rotate a certificate for a specific resource.
/// </summary>
/// <param name="ResourceId">The Azure resource ID where the certificate is used.</param>
/// <param name="CertificateName">The name of the certificate to rotate.</param>
/// <param name="ForceRotation">Whether to force rotation even if not needed.</param>
/// <param name="ValidateAfterRotation">Whether to validate the certificate after rotation.</param>
public record RotateCertificateCommand(
    string ResourceId,
    string CertificateName,
    bool ForceRotation = false,
    bool ValidateAfterRotation = true
) : ICommand<bool>;
