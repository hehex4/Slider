using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CaveSTile : STile
{
    [Header("Cave STile")]
    public CaveGrid grid;
    public Tilemap wallsSTilemap;

    private List<GameObject> objectsThatBlockLight;

    [SerializeField] private List<Vector2Int> validDirsForLight;

    public Texture2D HeightMask
    {
        get
        {
            if (_heightMask == null)
            {
                GenerateHeightMask();
            }
            return _heightMask;
        }
    }
    private Texture2D _heightMask;

    private void Awake()
    {
        // base.Awake();

        objectsThatBlockLight = new List<GameObject>();
        Transform[] objects = GetComponentsInChildren<Transform>(true); // true -> include inactive components
        foreach (var o in objects)
        {
            if (o.CompareTag("BlocksLight"))
            {
                objectsThatBlockLight.Add(o.gameObject);
            }
        }
    }

    private new void Start()
    {
        grid = SGrid.current as CaveGrid;

        base.Start();
    }

    private new void Update()
    {
        base.Update();

        if (LightManager.instance != null && this.GetMovingDirection() != Vector2.zero)
        {
            LightManager.instance.UpdateAll();
        }
    }

    public override void SetTileActive(bool isTileActive)
    {
        base.SetTileActive(isTileActive);
        
        if (isTileActive && LightManager.instance != null)
        {
            LightManager.instance.UpdateHeightMask(this);
            LightManager.instance.UpdateMaterials();
        }
    }

    public bool GetTileLit()
    {
        //Check if this tile has a light source
        CaveLight thisLight = GetComponentInChildren<CaveLight>();
        if (thisLight != null && thisLight.LightOn)
        {
            return true;
        }

        //Check if any valid adjacent tile has a light source
        foreach (var dir in validDirsForLight)
        {
            Vector2Int tileToCheck = new Vector2Int(this.x, this.y) + dir;

            //Border lights (hardcoded for now)
            if (tileToCheck == new Vector2Int(3, 2) || tileToCheck == new Vector2Int(2, -1))
            {
                return true;
            }
            if (tileToCheck.x >= 0 && tileToCheck.x < SGrid.current.width && tileToCheck.y >= 0 && tileToCheck.y < SGrid.current.height)
            {
                CaveSTile tile = (CaveSTile) SGrid.current.GetGrid()[tileToCheck.x, tileToCheck.y];
                CaveLight light = tile.GetComponentInChildren<CaveLight>();
                if (tile.isTileActive && light != null && light.LightOn)
                {
                    foreach (var lightDir in tile.validDirsForLight)
                    {
                        if (lightDir.x == -dir.x && lightDir.y == -dir.y)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    //L: Gets the STILE_WIDTH x STILE_WIDTH (17 x 17) height mask. (1 if there's a wall tile, 0 if not)
    private Texture2D GenerateHeightMask()
    {
        int offset = STILE_WIDTH / 2;
        _heightMask = new Texture2D(STILE_WIDTH, STILE_WIDTH);

        //L : Coordinates coorespond to the actual tile coordinates in the world, which are offset from the Texture2D coords by STILE_WIDTH / 2
        
        foreach (var go in objectsThatBlockLight)
        {
            Tilemap tm = go.GetComponent<Tilemap>();
            if (tm != null)
            {
                for (int x = -offset; x <= offset; x++)
                {
                    for (int y = -offset; y <= offset; y++)
                    {
                        TileBase tile = tm.GetTile(new Vector3Int(x, y, 0));
                        Color pixelColor = _heightMask.GetPixel(x + offset, y + offset);
                        // DC: Set the height to 1 if it was already 1, or tile isnt empty
                        _heightMask.SetPixel(x + offset, y + offset, (tile != null || pixelColor == Color.white) ? Color.white : Color.black);
                    }
                }
                // Debug.Log("Finished processing tilemap on " + name);
            } 
            else
            {
                //Position relative to the center of the tile
                Vector2Int posOnTile = new Vector2Int((int) (go.transform.position.x - transform.position.x), (int) (go.transform.position.y - transform.position.y));
                if (posOnTile.x < -offset || posOnTile.x > offset || posOnTile.y < -offset || posOnTile.y > offset)
                {
                    Debug.LogError("Positions when calculating height mask fall outside the tile's bounds");
                }
                // Debug.Log("Adding a wall at " + posOnTile.x + ", " + posOnTile.y);
                _heightMask.SetPixel(posOnTile.x + offset, posOnTile.y + offset, Color.white);
            }
        }
        

        _heightMask.Apply();
        return _heightMask;
    }
}
