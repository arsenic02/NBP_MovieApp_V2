using System;

public class Gledao{
       public string id { get; set; }
        public string KorisnikID { get; set; }
        public string FilmID{ get; set; }
        public string Status{ get; set; }//Zavrsio,trenutno gledam, odustao, plamniram da gledam

        public DateTime DatumPromeneStatusa{ get; set; }
}