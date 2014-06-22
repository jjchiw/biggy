namespace Biggy.Lucene.Tests.Models {
  public class Film {
	[PrimaryKey(true)]
    public int FilmId { get; set; }

    [FullText]
    public string Title { get; set; }

    [FullText]
    public string Description { get; set; }
    public int ReleaseYear { get; set; }
    public int Length { get; set; }
  }
}
