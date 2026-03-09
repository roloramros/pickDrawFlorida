using System;

namespace FloridaLotteryApp
{
    public class PatternRow
    {
        public string ReferenceNumber { get; set; } = " ";
        public string ReferenceDate { get; set; } = " ";
        public string ReferencePick3 { get; set; } = " ";
        public string ReferencePick4 { get; set; } = " ";
        public string ReferenceNextPick3 { get; set; } = " ";
        public string ReferenceDrawTime { get; set; } = " ";
        public string ReferenceCodificacion { get; set; } = " ";
        
// Columna 2 (Match)
        public string MatchNumber { get; set; } = " ";
        public string MatchPick3 { get; set; } = " ";
        public string MatchPick4 { get; set; } = " ";
        public string MatchNextPick3 { get; set; } = " ";
        public string MatchDrawTime { get; set; } = " ";
        public string MatchDate { get; set; } = " ";
        public string MatchCodificacion { get; set; } = " ";
        
        // Columna 3 (Similar)
        public string SimilarNumber { get; set; } = " ";
        public string SimilarPick3 { get; set; } = " ";
        public string SimilarPick4 { get; set; } = " ";
        public string SimilarNextPick3 { get; set; } = " ";
        public string SimilarDrawTime { get; set; } = " ";
        public string SimilarDate { get; set; } = " ";
        public string SimilarCodificacion { get; set; } = " ";
        
        // Columna 4 (SimilarMatch)
        public string SimilarMatchNumber { get; set; } = " ";
        public string SimilarMatchPick3 { get; set; } = " ";
        public string SimilarMatchPick4 { get; set; } = " ";
        public string SimilarMatchNextPick3 { get; set; } = " ";
        public string SimilarMatchDrawTime { get; set; } = " ";
        public string SimilarMatchDate { get; set; } = " ";
        public string SimilarMatchCodificacion { get; set; } = " ";
        
        // Propiedad adicional para patrones similares (si se necesita)
        public string SimilarPatternNumber { get; set; } = " ";
    }
}


