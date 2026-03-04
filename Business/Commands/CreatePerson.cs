using MediatR;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Common;
using StargateAPI.Business.Data;
using System.Net;

namespace StargateAPI.Business.Commands
{
    public class CreatePerson : IRequest<CreatePersonResult>
    {
        public required string Name { get; set; } = string.Empty;
    }

    public class CreatePersonHandler : IRequestHandler<CreatePerson, CreatePersonResult>
    {
        private readonly StargateContext _context;

        public CreatePersonHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<CreatePersonResult> Handle(
            CreatePerson request,
            CancellationToken cancellationToken)
        {
            var result = new CreatePersonResult();

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

                var exists = await _context.People
                    .AnyAsync(
                        p => p.Name.ToLower() == normalizedName.ToLower(),
                        cancellationToken);

                if (exists)
                {
                    result.Success = false;
                    result.Message = $"A person named '{normalizedName}' already exists.";
                    result.ResponseCode = (int)HttpStatusCode.BadRequest;
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
            catch (Exception)
            {
                result.Success = false;
                result.Message = "An unexpected error occurred.";
                result.ResponseCode = (int)HttpStatusCode.InternalServerError;
                return result;
            }
        }
    }

    public class CreatePersonResult : BaseResponse
    {
        public int Id { get; set; }
    }
}