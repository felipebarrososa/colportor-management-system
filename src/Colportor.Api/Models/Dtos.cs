namespace Colportor.Api.Models;
public record LoginDto(string Email, string Password);
public record TokenDto(string Token);
public record CreateColportorDto(string FullName, string CPF, string Country, string Region, string City, string? PhotoUrl, string Email, string Password);
public record VisitDto(int ColportorId, DateTime Date);
public record FilterDto(string? City, string? CPF, string? Status);
public record PacEnrollRequest(int[] ColportorIds, DateTime StartDate, DateTime EndDate);
