using Mediatr.Broker;

namespace client.slave
{
    public class SampleResponse
    {
    }

    public class SampleRequest : IRequest
    {
    }

    public class SampleHandler : Handler<SampleRequest>
    {
        public Task<Guid> Handle(SampleRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Guid Handle(SampleRequest request)
        {
            throw new NotImplementedException();
        }
    }
}