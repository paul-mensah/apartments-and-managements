using System.Collections.Generic;
using ApartmentsAndManagement.Api.Storage;

namespace ApartmentsAndManagement.Api.Actors.Messages;

public struct PropertiesPersistenceMessage
{
    public List<PropertyData> Documents { get; set; }

    public PropertiesPersistenceMessage(List<PropertyData> documents)
    {
        Documents = documents;
    }
}