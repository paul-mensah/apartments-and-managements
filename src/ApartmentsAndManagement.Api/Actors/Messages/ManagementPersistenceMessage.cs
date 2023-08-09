using System.Collections.Generic;
using ApartmentsAndManagement.Api.Storage;

namespace ApartmentsAndManagement.Api.Actors.Messages;

public struct ManagementPersistenceMessage
{
    public List<ManagementData> Documents { get; set; }

    public ManagementPersistenceMessage(List<ManagementData> documents)
    {
        Documents = documents;
    }
}