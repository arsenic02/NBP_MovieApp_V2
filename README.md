Web aplikacija omogućava korisnicima kreiranje javnih ili privatnih lista odgledanih filmova sa ocenama i komentarima. 
Za preporuku filmova koristi se Neo4J baza koja povezuje podatke o žanrovima, glumcima i ocenama, dok se za ocene, komentare i sistem obaveštenja koristi Redis sa publisher/subscriber modelom. 
Privatne liste imaju preporuke bazirane na najpopularnijim filmovima, dok javne liste utiču na prosečne ocene i prikaz komentara.
