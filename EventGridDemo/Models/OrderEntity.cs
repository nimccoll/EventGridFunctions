using Microsoft.Azure.Cosmos.Table;

namespace EventGridDemo.Models
{
    public class OrderEntity : TableEntity
    {
        public string CustomerId
        {
            get
            {
                return this.PartitionKey;
            }
            set
            {
                this.PartitionKey = value;
            }
        }

        public string OrderId
        {
            get
            {
                return this.RowKey;
            }
            set
            {
                this.RowKey = value;
            }
        }

        public string ProductId { get; set; }

        public int Quantity { get; set; }
    }
}
