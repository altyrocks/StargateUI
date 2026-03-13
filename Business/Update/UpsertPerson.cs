using MediatR;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Common;
using StargateAPI.Business.Data;
using System.Net;

namespace StargateAPI.Business.Update
{
    public class UpsertPerson : IRequest<UpsertPersonResult>
    {
        public required string Name { get; set; }
    }

    public class UpsertPersonHandler
        : IRequestHandler<UpsertPerson, UpsertPersonResult>
    {
        private readonly StargateContext _context;

        public UpsertPersonHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<UpsertPersonResult> Handle(
            UpsertPerson request,
            CancellationToken cancellationToken)
        {
            var result = new UpsertPersonResult();

            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    result.Success = false;
                    result.Message = "Name is required.";
                    result.ResponseCode = (int)HttpStatusCode.BadRequest;

                    return result;
                }

                var normalizedName = request.Name.Trim();

                var existing = await _context.People
                    .FirstOrDefaultAsync(
                        p => p.Name.ToLower() == normalizedName.ToLower(),
                        cancellationToken);

                if (existing is not null)
                {
                    if (!string.Equals(existing.Name, normalizedName, StringComparison.Ordinal))
                    {
                        existing.Name = normalizedName;

                        await _context.SaveChangesAsync(cancellationToken);
                    }

                    result.Success = true;
                    result.Message = "Person updated successfully.";
                    result.ResponseCode = (int)HttpStatusCode.OK;
                    result.Id = existing.Id;

                    return result;
                }

                var newPerson = new Person
                {
                    Name = normalizedName
                };

                await _context.People.AddAsync(newPerson, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                result.Success = true;
                result.Message = "Person created successfully.";
                result.ResponseCode = (int)HttpStatusCode.Created;
                result.Id = newPerson.Id;

                return result;
            }
            catch
            {
                result.Success = false;
                result.Message = "An unexpected error occurred.";
                result.ResponseCode = (int)HttpStatusCode.InternalServerError;

                return result;
            }
        }
    }

    public class UpsertPersonResult : BaseResponse
    {
        public int Id { get; set; }
    }
}