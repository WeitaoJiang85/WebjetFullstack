import React, { useState } from "react";
import { GiPriceTag } from "react-icons/gi";
import { Movie } from "../types";
import placeholderBackground from "../assets/landscape.jpg";

interface MovieDetailProps {
  movie: Movie | null;
}

const MovieDetail: React.FC<MovieDetailProps> = ({ movie }) => {
  if (!movie) {
    return (
      <div className="text-gray-400 text-lg p-4">
        Select a movie to see details
      </div>
    );
  }

  const [posterError, setPosterError] = useState(false);

  return (
    <div className="relative w-2/3 h-screen bg-black text-white">
      {/* Hidden Image for Error Handling */}
      <img
        src={movie.poster}
        alt={movie.title}
        className="hidden"
        onError={() => setPosterError(true)}
        onLoad={() => setPosterError(false)}
      />

      {/* Background Poster */}
      <div
        className="absolute inset-0 bg-cover bg-center opacity-50"
        style={{
          backgroundImage: `url(${posterError ? placeholderBackground : movie.poster})`,
        }}
      />

      {/* Movie Details */}
      <div className="relative p-6 bg-black bg-opacity-70 rounded-lg m-6">
        <h1 className="text-3xl font-bold text-yellow-400">{movie.title}</h1>
        <p>
          <strong>Year:</strong> {movie.year}
        </p>
        <p>
          <strong>Rated:</strong> {movie.rated}
        </p>
        <p>
          <strong>Released:</strong> {movie.released}
        </p>
        <p>
          <strong>Runtime:</strong> {movie.runtime}
        </p>
        <p>
          <strong>Genre:</strong> {movie.genre}
        </p>
        <p>
          <strong>Director:</strong> {movie.director}
        </p>
        <p>
          <strong>Writer:</strong> {movie.writer}
        </p>
        <p>
          <strong>Actors:</strong> {movie.actors}
        </p>
        <p>
          <strong>Plot:</strong> {movie.plot}
        </p>
        <p>
          <strong>Language:</strong> {movie.language}
        </p>
        <p>
          <strong>Country:</strong> {movie.country}
        </p>
        <p>
          <strong>Awards:</strong> {movie.awards}
        </p>
        <p>
          <strong>Metascore:</strong> {movie.metascore}
        </p>
        <p>
          <strong>Rating:</strong> {movie.Rating}
        </p>
        <p>
          <strong>Votes:</strong> {movie.Votes}
        </p>

        {/* Best Price */}
        {movie.FirstPrice !== undefined && (
          <p className="text-yellow-400 flex items-center mt-4">
            <GiPriceTag className="mr-2" />
            Best Price: ${movie.FirstPrice.toFixed(2)} (
            {movie.firstProvider.charAt(0).toUpperCase() +
              movie.firstProvider.slice(1)}
            )
          </p>
        )}

        {/* Other Price */}
        {movie.SecondPrice !== undefined &&
          movie.SecondPrice > 0 &&
          movie.secondProvider !== "unknown" &&
          movie.secondProvider !== "" && (
            <p className="text-gray-300 flex items-center">
              <GiPriceTag className="mr-2" />
              Other Price: ${movie.SecondPrice.toFixed(2)} (
              {(movie.secondProvider ?? "").charAt(0).toUpperCase() +
                (movie.secondProvider ?? "").slice(1)}
              )
            </p>
          )}
      </div>
    </div>
  );
};

export default MovieDetail;
