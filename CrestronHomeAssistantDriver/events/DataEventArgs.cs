namespace Crestron.HomeAssistant.Events
{
    public class DataEventArgs<T>
    {
        public T Data { get; private set; }

        public DataEventArgs(T data)
        {
            Data = data;
        }
    }
}
