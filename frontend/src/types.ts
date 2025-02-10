export interface Movie {
  rawID: string;
  title: string;
  year: string;
  rated: string;
  released: string;
  runtime: string;
  genre: string;
  director: string;
  writer: string;
  actors: string;
  plot: string;
  language: string;
  country: string;
  awards: string;
  poster: string;
  metascore: string;
  Rating: number;
  Votes: number;
  firstID: string;
  secondID?: string;
  type: string;
  FirstPrice: number;
  firstProvider: string;
  SecondPrice?: number;
  secondProvider?: string;
}
