using System.Collections.Generic;

public class Ocena
{
    public string id { get; set; }
    public int UnetaOcena { get; set; } //ocena koju korisnik unosi u input-u od 1-10
    public List<int> ListaOcena{get;set;}//lista ocena na osnovu koje se racuna prosecna ocena
    public float ProsecnaOcena{get;set;}
    public string MovieId{get;set;}
    public string UserId{get;set;}
}