import { useContext, useState, useEffect } from "react";
import { MovieContext } from "../context/MovieContext";
import MovieCard from "./MovieCard";
import SortFilter from "./SortFilter";

const MovieList = () => {
  const context = useContext(MovieContext);
  if (!context) return null;
  const {
    movies,
    faves,
    selectMovie,
    addToFaves,
    removeFromFaves,
    selectedMovie,
  } = context;

  const [activeTab, setActiveTab] = useState<"all" | "faves">("all");
  const [sortedMovies, setSortedMovies] = useState(movies);
  const [sortedFaves, setSortedFaves] = useState(faves);

  useEffect(() => {
    setSortedMovies(movies);
    handleSortChange("rating", "desc");
  }, [movies]);
  useEffect(() => {
    setSortedFaves(faves);
  }, [faves]);

  const handleSortChange = (sortType: string, sortOrder: "asc" | "desc") => {
    const sortList = (list: any[]) => {
      return [...list].sort((a, b) => {
        let valueA: any = a[sortType as keyof typeof a];
        let valueB: any = b[sortType as keyof typeof b];

        if (sortType === "year" || sortType === "votes") {
          valueA = parseInt(valueA);
          valueB = parseInt(valueB);
        } else if (sortType === "bestPrice") {
          valueA = a.firstPrice;
          valueB = b.firstPrice;
        } else if (sortType === "rating") {
          valueA = parseFloat(valueA);
          valueB = parseFloat(valueB);
        }

        return sortOrder === "asc" ? valueA - valueB : valueB - valueA;
      });
    };

    setSortedMovies(sortList(movies));
    setSortedFaves(sortList(faves));
  };

  const currentList = activeTab === "all" ? sortedMovies : sortedFaves;

  return (
    <div className="w-1/3 bg-gray-900 p-4 text-white h-screen overflow-y-auto">
      <div className="flex justify-between items-center mb-4">
        <button
          className={`px-4 py-2 rounded-lg font-bold transition ${
            activeTab === "all"
              ? "bg-yellow-500 text-black"
              : "bg-gray-700 text-white"
          }`}
          onClick={() => setActiveTab("all")}
        >
          ALL{" "}
          <span className="bg-black text-yellow-400 px-2 py-1 rounded-lg">
            {movies.length}
          </span>
        </button>

        <button
          className={`px-4 py-2 rounded-lg font-bold transition ${
            activeTab === "faves"
              ? "bg-yellow-500 text-black"
              : "bg-gray-700 text-white"
          }`}
          onClick={() => setActiveTab("faves")}
        >
          FAVES{" "}
          <span className="bg-black text-yellow-400 px-2 py-1 rounded-lg">
            {faves.length}
          </span>
        </button>
      </div>

      <SortFilter onSortChange={handleSortChange} />

      <div className="space-y-4 mt-4">
        {currentList.length > 0 ? (
          currentList.map((movie) => (
            <MovieCard
              key={movie.rawID}
              movie={movie}
              onClick={() => selectMovie(movie)}
              isSelected={selectedMovie?.rawID === movie.rawID}
              isFavorite={faves.some((fav) => fav.rawID === movie.rawID)}
              toggleFavorite={() =>
                faves.some((fav) => fav.rawID === movie.rawID)
                  ? removeFromFaves(movie)
                  : addToFaves(movie)
              }
            />
          ))
        ) : (
          <p className="text-center text-gray-400">No movies available</p>
        )}
      </div>
    </div>
  );
};

export default MovieList;
