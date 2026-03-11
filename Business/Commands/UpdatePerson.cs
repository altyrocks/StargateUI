using MediatR;
using System.Net;
using StargateAPI.Business.Data;
using StargateAPI.Business.Common;
using Microsoft.EntityFrameworkCore;

namespace StargateAPI.Business.Commands
{
    public class UpdatePerson : IRequest<BaseResponse>
    {
        public int Id { get; set; }
        public required string Name { get; set; }
    }

    public class UpdatePersonHandler : IRequestHandler<UpdatePerson, BaseResponse>
    {
        private readonly StargateContext _context;

        public UpdatePersonHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<BaseResponse> Handle(UpdatePerson request, CancellationToken cancellationToken)
        {
            var result = new BaseResponse();

            var person = await _context.People
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (person == null)
            {
                result.Success = false;
                result.Message = "Person not found.";
                result.ResponseCode = (int)HttpStatusCode.NotFound;

                return result;
            }

            var exists = await _context.People
                .AnyAsync(p => p.Name.ToLower() == request.Name.ToLower() && p.Id != request.Id,
                          cancellationToken);

            if (exists)
            {
                result.Success = false;
                result.Message = $"A person named '{request.Name}' already exists.";
                result.ResponseCode = (int)HttpStatusCode.BadRequest;

                return result;
            }

            person.Name = request.Name.Trim();

            await _context.SaveChangesAsync(cancellationToken);

            result.Success = true;
            result.Message = "Person updated successfully.";
            result.ResponseCode = (int)HttpStatusCode.OK;

            return result;
        }
    }
}