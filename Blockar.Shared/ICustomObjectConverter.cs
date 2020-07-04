namespace Daramee.Blockar
{
	public interface ICustomObjectConverter
	{
		void FromBlockarObject (BlockarObject obj);
		void ToBlockarObject (BlockarObject obj);
	}
}
