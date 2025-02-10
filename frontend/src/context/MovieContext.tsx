import React, { createContext, useState, useEffect } from "react";
import { Movie } from "../types";

interface MovieContextProps {
  movies: Movie[];
  faves: Movie[];
  selectedMovie: Movie | null;
  selectMovie: (movie: Movie) => void;
  addToFaves: (movie: Movie) => void;
  removeFromFaves: (movie: Movie) => void;
}

export const MovieContext = createContext<MovieContextProps | undefined>(
  undefined
);

export const MovieProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [movies, setMovies] = useState<Movie[]>([]);
  const [faves, setFaves] = useState<Movie[]>([]);
  const [selectedMovie, setSelectedMovie] = useState<Movie | null>(null);

  // Fetch movie details from API on mount
  useEffect(() => {
    const fetchMovies = async () => {
      try {
        console.log("Fetching movies...");
        const response = await fetch(
          "https://webjetfullstack-backend.onrender.com/api/mergedmoviedetails"
        );
        if (!response.ok) {
          throw new Error(`HTTP error! Status: ${response.status}`);
        }
        const data: Movie[] = await response.json();
        setMovies(data);
        if (data.length > 0) {
          setSelectedMovie(data[0]); // Select the first movie by default
        }
      } catch (error) {
        console.error("Failed to fetch movies:", error);
      }
    };

    fetchMovies();
  }, []);

  const selectMovie = (movie: Movie) => setSelectedMovie(movie);

  const addToFaves = (movie: Movie) => {
    setFaves((prevFaves) => [...prevFaves, movie]);
  };

  const removeFromFaves = (movie: Movie) => {
    setFaves((prevFaves) =>
      prevFaves.filter((fav) => fav.rawID !== movie.rawID)
    );
  };

  return (
    <MovieContext.Provider
      value={{
        movies,
        faves,
        selectedMovie,
        selectMovie,
        addToFaves,
        removeFromFaves,
      }}
    >
      {children}
    </MovieContext.Provider>
  );
};
