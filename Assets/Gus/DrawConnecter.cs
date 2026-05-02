using UnityEngine;
using System.Collections.Generic;
using StationO;

namespace DrawConnecter
{
    public class Draw : MonoBehaviour
    {
        //public string connecterLengthPiece = "./Assets/Gus/ConnecterLengthPath.obj";
        //public GameObject placedPiece = OtherSpaceStationPeices.AttPieces.AttachedPiece;

        public GameObject connectorLengthPrefab; // The ":" filler pieces
        public float pieceLengthUnits = 1.0f;    // How long each ":" piece is in world units

        private List<GameObject> spawnedPieces = new List<GameObject>();

        // Ex: the amount of ":" pieces to make the connection length - connecter length piece.
        // The connecter piece has 1 more length than needed so it's hidden inside the attached piece
        // Total example: :::::::::

        public void DrawConnector(Vector3 position)
        { //Might need to be changed to a DrawConnecter(selectedPiece) because you already know where the root is (0,0)
            // --- 1. Get world positions of the root and attached pieces ---
            Vector3 startPos = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 endPos = position;

            // --- 2. Calculate direction and total distance ---
            Vector3 direction = endPos - startPos;
            float totalDistance = direction.magnitude;
            Vector3 normalizedDir = direction.normalized;

            // --- 3. Figure out how many ":" length pieces are needed (+1 extra so the end is hidden inside the attached piece) ---
            int lengthPieceCount = Mathf.RoundToInt(direction.magnitude / 1.0f) + 1;

            // --- 4. Calculate rotation so pieces face from root to attached ---
            Quaternion connectorRotation = Quaternion.LookRotation(normalizedDir);

            // --- 5. Clear any previously drawn connector pieces ---
            ClearConnector();

            // --- 6. Spawn each ":" length piece along the path ---
            for (int i = 0; i < lengthPieceCount; i++)
            {
                float offset = (i * pieceLengthUnits) + (pieceLengthUnits / 2f);
                Vector3 piecePos = startPos + normalizedDir * offset;
                SpawnPiece(connectorLengthPrefab, piecePos, connectorRotation);
            }
        }

        // Instantiates a piece, tracks it, and parents it to this GameObject for easy cleanup
        public void SpawnPiece(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogWarning("DrawConnecter: connectorLengthPrefab is not assigned in the Inspector.");
                return;
            }

            GameObject piece = Instantiate(prefab, position, rotation);
            piece.transform.SetParent(this.transform);
            spawnedPieces.Add(piece);
        }

        // Destroys all previously spawned connector pieces so we can redraw cleanly
        public void ClearConnector()
        {
            foreach (GameObject piece in spawnedPieces)
            {
                if (piece != null)
                    Destroy(piece);
            }
            spawnedPieces.Clear();
        }
    }
}