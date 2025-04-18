﻿using Coil.Api.Shared;
namespace Coil.Api.Shared.MediatR
{
    public interface IRequest<TResponse>
    {
    }
    public interface IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }
}
