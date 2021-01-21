namespace Matrix.Common.Maquette
{
	public class MaquetteInfo
	{
		public string Content { get; private set; }
		public string Name { get; private set; }

		public MaquetteInfo(string content, string name)
		{
			Content = content;
			Name = name;
		}
	}
}
