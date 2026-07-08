namespace PagueVeloz.Application.DTOs.Requests;

public record CaptureAccountRequest(Guid ReserveOperationId, string ReferenceId);
