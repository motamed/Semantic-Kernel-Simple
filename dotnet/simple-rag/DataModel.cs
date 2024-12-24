using Microsoft.Extensions.VectorData;

namespace MyDataModel
{

    sealed class DocumentData
    {
        [VectorStoreRecordKey]
        public string? Key { get; set; }

        [VectorStoreRecordData]
        public string? content { get; set; }
       
        [VectorStoreRecordVector(768)]  // Setting the diemnsion of the embedding to 1024 based on the model used 'mxbai-embed-large'
        public ReadOnlyMemory<float>? ContentEmbedding { get; set; }
    }
}

