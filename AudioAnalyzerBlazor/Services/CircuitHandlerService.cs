using Microsoft.AspNetCore.Components.Server.Circuits;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public class CircuitHandlerService : CircuitHandler
{
    public ConcurrentDictionary<string, Circuit> Circuits { get; set; }

    private readonly UploadedFilesState _uploadedFileState;

    public CircuitHandlerService(UploadedFilesState uploadedFilesState)
    {
        Circuits = new ConcurrentDictionary<string, Circuit>();
        _uploadedFileState = uploadedFilesState;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Circuits[circuit.Id] = circuit;
        return base.OnCircuitOpenedAsync(circuit, cancellationToken);
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Circuit circuitRemoved;
        Console.WriteLine("Circuit closed");
        foreach (var upload in _uploadedFileState.UploadedFiles)
        {
            while (true)
            {
                try
                {
                    File.Delete(upload.FileName);
                    break;
                }
                catch (DirectoryNotFoundException e)
                {
                    Console.WriteLine(e);
                    break;
                }
                catch
                {
                    continue;
                }
            }
        }
        Circuits.TryRemove(circuit.Id, out circuitRemoved);
        return base.OnCircuitClosedAsync(circuit, cancellationToken);
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Console.WriteLine("Circuit connection down");
        return base.OnConnectionDownAsync(circuit, cancellationToken);
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        return base.OnConnectionUpAsync(circuit, cancellationToken);
    }
}