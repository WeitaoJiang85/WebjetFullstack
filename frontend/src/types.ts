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
  rating: number;
  votes: number;
  firstID: string;
  secondID?: string;
  type: string;
  firstPrice: number;
  firstProvider: string;
  secondPrice?: number;
  secondProvider?: string;
}
