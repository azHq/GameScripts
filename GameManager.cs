using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public GameObject player;
    private GameObject game_player;
    private Animator anim;
    public GameObject wumpus, gold,pit,breeze,stench,breeze_stench,visited,bullet2;
    private GameObject bullet;
    private float startPointX = -3.66f, startPointY = 4.52f;
    private float differenceX = 1.23f, differenceY = 1f;
    float[,,] board = new float[10,10,3];
    public int numberOfWumpus = 3, numberOfPit=5, numberOfGold=2;
    GameObject[,] gameObjects = new GameObject[10,10];
    GameObject[,] visited_cell = new GameObject[10, 10];
    private float source = 0.0f, destination=0.0f, w_source, w_destination;
    private int[] player_currentPosition = new int[2];
    private int[] player_previousPosition = new int[2];
    private int[] wumpus_index = new int[2];
    private float speed = .02f;
    private Path[,,] path = new Path[10, 10, 4];
    public Cell_State[,] KB = new Cell_State[10, 10];
    private bool start = false;
    private bool movingStart = true,bullet_move=false;
    private Stack<int[]> visited_cell_track = new Stack<int[]>();
    int numOfshoot = 0;
    public Text bulletText, goldText, costText;
    public int goldNum = 0,cost=0;
    String filepath;
    void Start()
    {
        filepath = Application.dataPath + "/KB.txt";
        //File.WriteAllText(filepath, "");

        boardPointInit();
        boardContentInit();
        numOfshoot = numberOfWumpus;
        goldNum = numberOfGold;



    }

    // Update is called once per frame
    void Update()
    {


        if (start)
        {
            if (movingStart&& !bullet_move)
            {
                StartCoroutine(wait());
               
            }
            else if(anim != null&& !movingStart&& !bullet_move) run();

            if (bullet_move && bullet!=null)
            {
                
                anim.SetBool("down", false); 
                anim.SetBool("left_move", false);   
                anim.SetBool("right_move", false);
                anim.SetBool("up", false);
                bulletMove();
               
            }
        }

        bulletText.text = "Bullet: " + numOfshoot;
        goldText.text = "Gold: " + goldNum;
        costText.text = "Cost: " + cost;


    }

    IEnumerator wait()
    {
        movingStart = false;
        yield return new WaitForSeconds(.5f);
        int[] wumpus_position = checkWumpus();
        if (wumpus_position[0] != -1 && numOfshoot > 0) shoot(wumpus_position);
        else moveNextCell();


    }
    public void moveNextCell()
    {
        int[] index = findSafeCell();     
        if (index[0]!=-1)
        {
            if (board[index[0], index[1], 0] == Constant.BREEZE) updateKB(index, Constant.BREEZE);
            else if (board[index[0], index[1], 0] == Constant.STENCH) updateKB(index, Constant.STENCH);
            else if (board[index[0], index[1], 0] == Constant.PIT) updateKB(index, Constant.PIT);
            else if (board[index[0], index[1], 0] == Constant.WUMPUS) updateKB(index, Constant.WUMPUS);
            else if (board[index[0], index[1], 0] == Constant.BREEZE_STENCH)
            {
                updateKB(index, Constant.BREEZE);
                updateKB(index, Constant.STENCH);
            }
            else if (board[index[0], index[1], 0] == Constant.EMPTY) updateKB(index, Constant.EMPTY);
            else if(board[index[0], index[1], 0] == Constant.GOLD) updateKB(index, Constant.GOLD);

            chooseAnimation_And_Move(index);
           
        }
        

    }
    public void chooseAnimation_And_Move(int[] index)
    {

        
       
        // game_player.transform.position = new Vector3(board[index[0], index[1],1], board[index[0], index[1], 2], -1f);
        if (index[1] > player_currentPosition[1])
         {
            source = board[player_currentPosition[0], player_currentPosition[1], 1];
            destination = board[index[0], index[1], 1];
            print("move right");
            anim.SetBool("right_move", false);
            anim.SetBool("right_turn", true);
            right_turnAI();
         }
         else if (index[1] < player_currentPosition[1])
         {
            source = board[player_currentPosition[0], player_currentPosition[1], 1];
            destination = board[index[0], index[1], 1];
            print("move left");
             anim.SetBool("left_move", false);
             anim.SetBool("left_turn", true);
             left_turnAI();
         }
         else if (index[0] < player_currentPosition[0])
         {
            source = board[player_currentPosition[0], player_currentPosition[1], 2];
            destination = board[index[0], index[1], 2];
            print("move up");
             anim.SetBool("up", false);
             anim.SetBool("up_turn", true);
             up_turnAI();
         }
         else if (index[0] > player_currentPosition[0])
         {
            source = board[player_currentPosition[0], player_currentPosition[1], 2];
            destination = board[index[0], index[1], 2];
            print("move down");
             anim.SetBool("down", false);
             anim.SetBool("down_turn", true);
             down_turnAI();
         }
        player_previousPosition[0] = player_currentPosition[0];
        player_previousPosition[1] = player_currentPosition[1]; 
        player_currentPosition[0] = index[0];
        player_currentPosition[1] = index[1];
        print("source(" + player_previousPosition[0] + "," + player_previousPosition[1] + ")" + " dest(" + player_currentPosition[0] + "," + player_currentPosition[1] + ")");
        if (visited_cell[player_previousPosition[0], player_previousPosition[1]] ==null) visited_cell[player_previousPosition[0], player_previousPosition[1]] = Instantiate(visited, new Vector3(board[player_previousPosition[0], player_previousPosition[1], 1], board[player_previousPosition[0], player_previousPosition[1], 2], 0f), visited.transform.rotation);
    }
    public void updateKB(int[] index,float type)
    {
        if (type == Constant.BREEZE)
        {
            KB[index[0], index[1]].breeze = true;
            KB[index[0], index[1]].isSafe = true;
            KB[index[0], index[1]].wumpus = false;
            KB[index[0], index[1]].pit = false;
        }
        else if (type == Constant.STENCH)
        {
            KB[index[0], index[1]].stench = true;     
            KB[index[0], index[1]].isSafe = true;
            KB[index[0], index[1]].wumpus = false;
            KB[index[0], index[1]].pit = false;
        }
        else if (type == Constant.WUMPUS)
        {
            KB[index[0], index[1]].wumpus = true;
            KB[index[0], index[1]].breeze = false;
            KB[index[0], index[1]].stench = false;
            KB[index[0], index[1]].isSafe = false;

        }
        else if (type == Constant.PIT)
        {
            KB[index[0], index[1]].pit = true;
            KB[index[0], index[1]].stench = false;
            KB[index[0], index[1]].breeze = false;
            KB[index[0], index[1]].isSafe = false;
        }

        else if(type == Constant.EMPTY)
        {
            KB[index[0], index[1]].isSafe = true;
            KB[index[0], index[1]].breeze = false;
            KB[index[0], index[1]].wumpus = false;
            KB[index[0], index[1]].pit = false;
            KB[index[0], index[1]].stench = false;
        }
        else if (type == Constant.GOLD)
        {
            KB[index[0], index[1]].isSafe = true;
            KB[index[0], index[1]].wumpus = false;
            KB[index[0], index[1]].pit = false;
            KB[index[0], index[1]].breeze = false;
            KB[index[0], index[1]].stench = false;
            goldNum--;
            cost += 100;
        }
       
        KB[index[0], index[1]].visited = true;
        for (int i = 0; i < 4; i++)
        {
            int row = path[index[0],index[1], i].row;
            int col = path[index[0], index[1], i].col;
            if (row != -1)
            {
                Cell_State cell;
                if (KB[row, col] == null) cell= new Cell_State();
                else cell = KB[row, col];

                if (!cell.visited)
                {
                    if (type == Constant.BREEZE)
                    {
                        cell.pit = true;
                        cell.isSafe = false;
                    }

                    else if (type == Constant.STENCH)
                    {
                        cell.wumpus = true;
                        cell.isSafe = false;
                    }

                    else if (type == Constant.WUMPUS)
                    {
                        cell.stench = true;
                        cell.isSafe = true;
                    }
                    else if (type == Constant.PIT)
                    {
                        cell.breeze = true;
                        cell.isSafe = true;
                    }
                    else if (type == Constant.EMPTY)
                    {
                        cell.isSafe = true;
                        cell.pit = false;
                        cell.wumpus = false;
                    }
                    else if (type == Constant.GOLD)
                    {
                        cell.isSafe = true;
                        cell.pit = false;
                        cell.wumpus = false;
                    }
                    KB[row, col] = cell;
                }
               
            }
        }
    }
    public int[] findSafeCell()
    {
        int[] index = { -1, -1 };
        entail();

        for (int i=0;i<4;i++)
        {
            int row = path[player_currentPosition[0], player_currentPosition[1], i].row;
            int col = path[player_currentPosition[0], player_currentPosition[1], i].col;
            if (row != -1 && KB[row, col] != null && KB[row, col].isSafe&& !KB[row, col].visited) {

                index[0]=row;
                index[1] = col;
                int[] node = { player_currentPosition[0], player_currentPosition[1]};
                visited_cell_track.Push(node);
                return index;
            }
        }
        while (visited_cell_track.Count>0)
        {
            int[] node=visited_cell_track.Pop();
            if (KB[node[0], node[1]].isSafe)
            {
                index[0] = node[0];
                index[1] = node[1];
                return index;
            }
            
          
        }
        for (int i = 0; i < 4; i++)
        {
            int row = path[player_currentPosition[0], player_currentPosition[1], i].row;
            int col = path[player_currentPosition[0], player_currentPosition[1], i].col;
            if (row != -1 && KB[row, col] != null && KB[row, col].isSafe)
            {

                index[0] = row;
                index[1] = col;
                return index;
            }
        }

        return index;
    }

    public void shoot(int[] index)
    {
        cost -= 10;
        numOfshoot--;
        movingStart = true;
        wumpus_index = index;
        if (index[0]<player_currentPosition[0])
        {
            source = board[player_currentPosition[0], player_currentPosition[1], 2];
            destination = board[index[0], index[1], 2];
            anim.SetBool("down_turn",true);

            anim.SetBool("up_turn",false);
            anim.SetBool("left_turn", false);
            anim.SetBool("right_turn", false);
        }
        else if (index[0] > player_currentPosition[0])
        {
            source = board[player_currentPosition[0], player_currentPosition[1], 2];
            destination = board[index[0], index[1], 2];
            anim.SetBool("up_turn", true);

            anim.SetBool("down_turn", false);
            anim.SetBool("left_turn", false);
            anim.SetBool("right_turn", false);
        }
        else if (index[1] < player_currentPosition[1])
        {
            source = board[player_currentPosition[0], player_currentPosition[1], 1];
            destination = board[index[0], index[1], 1];
            anim.SetBool("left_turn", true);

            anim.SetBool("up_turn", false);
            anim.SetBool("down_turn", false);
            anim.SetBool("right_turn", false);
        }
        else if (index[1] > player_currentPosition[1])
        {
            source = board[player_currentPosition[0], player_currentPosition[1], 1];
            destination = board[index[0], index[1], 1];
            anim.SetBool("right_turn", true);

            anim.SetBool("up_turn", false);
            anim.SetBool("left_turn", false);
            anim.SetBool("down_turn", false);
        }
        bullet = Instantiate(bullet2, new Vector3(board[player_currentPosition[0], player_currentPosition[1], 1], board[player_currentPosition[0], player_currentPosition[1], 2], -1f), bullet2.transform.rotation);
        bullet_move = true;
    }

    public void bulletMove()
    {
        if (anim.GetBool("right_turn"))
        {

            if (w_source <= w_destination)
            {
                w_source += speed;
                bullet.transform.position = new Vector3(w_source, bullet.transform.position.y, -1f);
            }
            else
            {
                Destroy(bullet);
                Destroy(gameObjects[wumpus_index[0], wumpus_index[1]]);
                KB[wumpus_index[0], wumpus_index[1]].wumpus = false;
                bullet_move = false;
                cost += 100;
            }
        }
        else if (anim.GetBool("left_turn"))
        {
            if (w_source >= w_destination)
            {
                w_source -= speed;
                bullet.transform.position = new Vector3(w_source, bullet.transform.position.y, -1f);

            }
            else
            {
                Destroy(bullet);
                Destroy(gameObjects[wumpus_index[0], wumpus_index[1]]);
                KB[wumpus_index[0], wumpus_index[1]].isSafe = true;
                KB[wumpus_index[0], wumpus_index[1]].wumpus = false;
                bullet_move = false;
                cost += 100;
            }
        }
        else if (anim.GetBool("up_turn"))
        {
            if (w_source <= w_destination)
            {
                w_source += speed;
                bullet.transform.position = new Vector3(bullet.transform.position.x, w_source, -1f);
            }
            else
            {
                Destroy(bullet);
                Destroy(gameObjects[wumpus_index[0], wumpus_index[1]]);
                KB[wumpus_index[0], wumpus_index[1]].wumpus = false;
                bullet_move = false;
                cost += 100;
            }

        }
        else if (anim.GetBool("down_turn"))
        {
            if (w_source >= w_destination)
            {
                w_source -= speed;
                bullet.transform.position = new Vector3(bullet.transform.position.x, w_source, -1f);
            }
            else
            {
                Destroy(bullet);
                Destroy(gameObjects[wumpus_index[0], wumpus_index[1]]);
                KB[wumpus_index[0], wumpus_index[1]].wumpus = false;
                bullet_move = false;
                cost += 100;
            }
        }
    }

    public int[] checkWumpus()
    {
        int[] index = { -1, -1 };
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                if (KB[i, j] != null &&  KB[i, j].wumpus&&(i==player_currentPosition[0]|| j == player_currentPosition[1]))
                {
                    int count = 0;
                    for (int k = 0; k < 4; k++)
                    {
                        int row = path[i, j, k].row;
                        int col = path[i, j, k].col;
                        if (row != -1 && KB[row, col] != null&&KB[row, col].stench&& KB[row, col].visited)
                        {
                            count++;
                        }
                        
                        
                    }

                    if (count >= 3) {

                        index[0] = i;
                        index[1] = j;
                        KB[i, j].isSafe = true;
                        KB[i, j].wumpus = false;
                        for (int k = 0; k < 4; k++)
                        {
                            int row = path[index[0], index[1], k].row;
                            int col = path[index[0], index[1], k].col;
                            if (row != -1 && KB[row, col] != null && KB[row, col].stench)
                            {
                                KB[row, col].stench = false;
                            }


                        }
                        return index;
                    }
                   
                }

            }
        }
        
       
        return index;
        
    }
    public void entail()
    {
       
       /* for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                if (KB[i, j] != null )
                {
                    
                            File.AppendAllText(filepath,"index:("+i+","+j+") pit:"+ KB[i, j].pit+ " wumpus:"+ KB[i, j].wumpus + " breeze:" + KB[i, j].breeze + " stench:" + KB[i, j].stench+" isSafe:"+ KB[i, j].isSafe+"  ");
                          


                }
                else
                {
                            File.AppendAllText(filepath, "                                  index:(" + i + "," + j + ")unknown        ");
                }

            }
            File.AppendAllText(filepath, "\n\n");
        }*/
       
        for (int i=0;i<10;i++)
        {
            for (int j=0;j<10;j++)
            {
                if (KB[i,j]!=null&&(KB[i, j].pit|| KB[i, j].wumpus)) {
                    for (int k = 0; k < 4; k++)
                    {
                        int row = path[i, j, k].row;
                        int col = path[i, j, k].col;
                        if (row!=-1&&KB[row,col] != null&& KB[row, col].visited)
                        {
                            KB[i, j].pit &= KB[row, col].breeze;
                            KB[i, j].wumpus &= KB[row, col].stench;
                            KB[i, j].isSafe = !(KB[i, j].wumpus | KB[i, j].pit);
                           
                           
                        }
                    }

                  //  File.AppendAllText(filepath, "index: (" + i + ", " + j + ") isSafe" + KB[i, j].isSafe);
                }
                
            }
        }
       // File.AppendAllText(filepath, "\n\n\n\n\n");
    }

    private void boardContentInit()
    {
        
        int i = 0;
        while (i< numberOfWumpus)
        {
            int randI = UnityEngine.Random.Range(0,10);
            int randJ = UnityEngine.Random.Range(0, 10);
            if (board[randI, randJ, 0]==0)
            {
                gameObjects[randI, randJ] =Instantiate(wumpus, new Vector3(board[randI, randJ, 1], board[randI, randJ, 2], -1f), wumpus.transform.rotation);
                board[randI, randJ, 0] = Constant.WUMPUS;
                i++;

                for (int j=0;j<4;j++)
                {
                    if (path[randI, randJ, j].row != -1)
                    {
                        if (board[path[randI, randJ, j].row, path[randI, randJ, j].col, 0] == 0)
                        {
                            board[path[randI, randJ, j].row, path[randI, randJ, j].col, 0] = Constant.STENCH;
                            gameObjects[path[randI, randJ, j].row, path[randI, randJ, j].col] = Instantiate(stench, new Vector3(board[path[randI, randJ, j].row, path[randI, randJ, j].col, 1], board[path[randI, randJ, j].row, path[randI, randJ, j].col, 2], -1f), stench.transform.rotation);
                           
                        }
                        else if (board[path[randI, randJ, j].row, path[randI, randJ, j].col, 0] == Constant.BREEZE)
                        {
                            board[path[randI, randJ, j].row, path[randI, randJ, j].col, 0] = Constant.BREEZE_STENCH;
                            Destroy(gameObjects[path[randI, randJ, j].row, path[randI, randJ, j].col]);
                            gameObjects[path[randI, randJ, j].row, path[randI, randJ, j].col] = Instantiate(breeze_stench, new Vector3(board[path[randI, randJ, j].row, path[randI, randJ, j].col, 1], board[path[randI, randJ, j].row, path[randI, randJ, j].col, 2], -1f), stench.transform.rotation);
                        }

                    }
                }

            }
        }
        i = 0;
        while (i < numberOfPit)
        {
            int randI = UnityEngine.Random.Range(0, 10);
            int randJ = UnityEngine.Random.Range(0, 10);
            if (board[randI, randJ, 0] == 0)
            {
                gameObjects[randI, randJ]=Instantiate(pit, new Vector3(board[randI, randJ, 1], board[randI, randJ, 2], -1f), wumpus.transform.rotation);
                board[randI, randJ, 0] = Constant.PIT;
                i++;

                for (int j = 0; j < 4; j++)
                {
                    if (path[randI, randJ, j].row != -1)
                    {
                        if (board[path[randI, randJ, j].row, path[randI, randJ, j].col, 0] == 0)
                        {
                            board[path[randI, randJ, j].row, path[randI, randJ, j].col, 0] = Constant.BREEZE;
                            gameObjects[path[randI, randJ, j].row, path[randI, randJ, j].col] = Instantiate(breeze, new Vector3(board[path[randI, randJ, j].row, path[randI, randJ, j].col, 1], board[path[randI, randJ, j].row, path[randI, randJ, j].col, 2], -1f), breeze.transform.rotation);

                        }
                        else if (board[path[randI, randJ, j].row, path[randI, randJ, j].col, 0] == Constant.STENCH)
                        {
                            board[path[randI, randJ, j].row, path[randI, randJ, j].col, 0] = Constant.BREEZE_STENCH;
                            Destroy(gameObjects[path[randI, randJ, j].row, path[randI, randJ, j].col]);
                            gameObjects[path[randI, randJ, j].row, path[randI, randJ, j].col] = Instantiate(breeze_stench, new Vector3(board[path[randI, randJ, j].row, path[randI, randJ, j].col, 1], board[path[randI, randJ, j].row, path[randI, randJ, j].col, 2], -1f), breeze.transform.rotation);
                        }

                    }
                }
            }
        }
        i = 0;
        while (i < numberOfGold)
        {
            int randI = UnityEngine.Random.Range(0, 10);
            int randJ = UnityEngine.Random.Range(0, 10);
            if (board[randI, randJ, 0] == 0)
            {
                gameObjects[randI, randJ] = Instantiate(gold, new Vector3(board[randI, randJ, 1], board[randI, randJ, 2], -1f), wumpus.transform.rotation);
                board[randI, randJ, 0] = Constant.GOLD;
                i++;
            }
        }


        //player
        while (true)
        {
            int randI = UnityEngine.Random.Range(0, 10);
            int randJ = UnityEngine.Random.Range(0, 10);
            if (board[randI, randJ, 0] == 0)
            {
                player_currentPosition[0] = randI;
                player_currentPosition[1] = randJ;
                Cell_State cell = new Cell_State();
                cell.isSafe = true;
                cell.breeze = false;
                cell.stench = false;
                cell.pit = false;
                cell.wumpus = false;
                KB[randI, randJ] = cell;
                int[] node = { randI, randJ };
                visited_cell_track.Push(node);
                // visited_cell[randI, randJ]= Instantiate(visited, new Vector3(board[randI, randJ, 1], board[randI, randJ, 2], -1f), visited.transform.rotation);
                updateKB(player_currentPosition, Constant.EMPTY);
                game_player = Instantiate(player, new Vector3(board[randI, randJ, 1], board[randI, randJ, 2], 0f), player.transform.rotation);
                anim = game_player.GetComponent<Animator>();
                break;
            }
        }
    }
    private void boardPointInit()
    {

        float pointX = startPointX, pointY = startPointY;
        for (int i = 0; i < 10; i++)
        {
            pointX = startPointX;
            for (int j = 0; j < 10; j++)
            {
                board[i, j, 0] = 0;
                board[i, j, 1] = pointX;
                board[i, j, 2] = pointY;
                pointX += differenceX;
            }
            pointY -= differenceY;
        }
        for (int i=0;i<10;i++)
        {
            for (int j=0;j<10;j++)
            {
                if (j-1 >= 0) path[i, j, 0] = new Path(i, j-1);
                else path[i, j, 0] = new Path(-1,-1);
                if(i-1>=0) path[i, j, 1] = new Path(i-1, j);
                else path[i, j, 1] = new Path(- 1,-1);
                if (j+ 1 <=9) path[i, j, 2] = new Path(i, j+1);
                else path[i, j, 2] = new Path(-1, -1);
                if (i + 1 <= 9) path[i, j, 3] = new Path(i+1, j);
                else path[i, j, 3] = new Path(-1, -1);

            }
        }

        


    }

    public void restart()
    {
         for (int i=0; i<10;i++)
         {
             for (int j=0;j<10; j++)
             {
                 if (board[i, j, 0] != 0)
                 {
                     Destroy(gameObjects[i, j]);
                     
                     Destroy(game_player);
                     board[i, j, 0] = 0;
                 }
                Destroy(visited_cell[i, j]);
            }

         }
        numOfshoot = numberOfWumpus;
        goldNum = numberOfGold;
        boardContentInit();
       
    }

    public void gameStart()
    {
        start = true;
    }
    public void gameStop()
    {
        start = false;
    }

    public void left_turnAI()
    {
        cost -= 5;
        anim.SetBool("left_turn", true);
        anim.SetBool("left_move", true);

        anim.SetBool("right_turn", false);
        anim.SetBool("right_move", false);
        anim.SetBool("up_turn", false);
        anim.SetBool("up",false);
        anim.SetBool("down_turn", false);
        anim.SetBool("down", false);
    }
    public void right_turnAI()
    {
        cost -= 5;
        anim.SetBool("right_turn", true);
        anim.SetBool("right_move", true);

        anim.SetBool("left_turn", false);
        anim.SetBool("left_move", false);
        anim.SetBool("up_turn", false);
        anim.SetBool("up", false);
        anim.SetBool("down_turn", false);
        anim.SetBool("down", false);
    }
    public void up_turnAI()
    {
        cost -= 5;
        anim.SetBool("up_turn", true);
        anim.SetBool("up", true);

        anim.SetBool("left_turn", false);
        anim.SetBool("left_move", false);
        anim.SetBool("right_turn", false);
        anim.SetBool("right_move", false);
        anim.SetBool("down_turn", false);
        anim.SetBool("down", false);
    }
    public void down_turnAI()
    {
        cost -= 5;
        anim.SetBool("down_turn", true);
        anim.SetBool("down", true);

        anim.SetBool("left_turn", false);
        anim.SetBool("left_move", false);
        anim.SetBool("right_turn", false);
        anim.SetBool("right_move", false);
        anim.SetBool("up_turn", false);
        anim.SetBool("up", false);

    }
    /* public void up_turn()
     {

         if (!anim.GetBool("up_turn"))
         {
             anim.SetBool("up", false);
             anim.SetBool("up_turn", true);
         }
         else
         {
             if (player_currentPosition[0] - 1>=0) {

                 if(board[player_currentPosition[0] - 1, player_currentPosition[1], 0] ==Constant.BREEZE) board[player_currentPosition[0] - 1, player_currentPosition[1], 0] = Constant.PLAYER_BREEZE;
                 else if(board[player_currentPosition[0] - 1, player_currentPosition[1], 0] == Constant.STENCH) board[player_currentPosition[0] - 1, player_currentPosition[1], 0] = Constant.PLAYER_STENCH;
                 else if(board[player_currentPosition[0] - 1, player_currentPosition[1], 0]==0) board[player_currentPosition[0] - 1, player_currentPosition[1], 0] = Constant.PLAYER;

                 if(board[player_currentPosition[0], player_currentPosition[1], 0]==Constant.PLAYER_BREEZE) board[player_currentPosition[0], player_currentPosition[1], 0] = Constant.BREEZE;
                 else if (board[player_currentPosition[0], player_currentPosition[1], 0] == Constant.PLAYER_STENCH) board[player_currentPosition[0], player_currentPosition[1], 0] = Constant.STENCH;
                 else if(board[player_currentPosition[0], player_currentPosition[1], 0] == Constant.PLAYER) board[player_currentPosition[0], player_currentPosition[1], 0] = 0;
                 source = board[player_currentPosition[0], player_currentPosition[1], 2];
                 destination = board[player_currentPosition[0]-1, player_currentPosition[1], 2];
                 player_previousPosition[0] = player_currentPosition[0];
                 player_previousPosition[1] = player_currentPosition[1];
                 player_currentPosition[0] -= 1;
                 anim.SetBool("up_turn", false);
                 anim.SetBool("up", true);
             }

         }
     }

     public void up_move()
     {

         anim.SetBool("up",true);
     }

     public void down_turn()
     {
         if (!anim.GetBool("down_turn"))
         {
             anim.SetBool("down", false);
             anim.SetBool("down_turn", true);
         }
         else
         {
             if (player_currentPosition[0] + 1 <10 )
             {
                 if (board[player_currentPosition[0] + 1, player_currentPosition[1], 0] == Constant.BREEZE) board[player_currentPosition[0] + 1, player_currentPosition[1], 0] = Constant.PLAYER_BREEZE;
                 else if (board[player_currentPosition[0] + 1, player_currentPosition[1], 0] == Constant.STENCH) board[player_currentPosition[0] + 1, player_currentPosition[1], 0] = Constant.PLAYER_STENCH;
                 else if (board[player_currentPosition[0] + 1, player_currentPosition[1], 0] == 0) board[player_currentPosition[0] + 1, player_currentPosition[1], 0] = Constant.PLAYER;

                 if (board[player_currentPosition[0], player_currentPosition[1], 0] == Constant.PLAYER_BREEZE) board[player_currentPosition[0], player_currentPosition[1], 0] = Constant.BREEZE;
                 else if (board[player_currentPosition[0], player_currentPosition[1], 0] == Constant.PLAYER_STENCH) board[player_currentPosition[0], player_currentPosition[1], 0] = Constant.STENCH;
                 else if (board[player_currentPosition[0], player_currentPosition[1], 0] == Constant.PLAYER) board[player_currentPosition[0], player_currentPosition[1], 0] = 0;

                 source = board[player_currentPosition[0], player_currentPosition[1], 2];
                 destination = board[player_currentPosition[0] + 1, player_currentPosition[1], 2];
                 player_previousPosition[0] = player_currentPosition[0];
                 player_previousPosition[1] = player_currentPosition[1];
                 player_currentPosition[0] += 1;
                 anim.SetBool("down_turn", false);
                 anim.SetBool("down", true);
             }

         }
     }

     public void down_move()
     {

     }

     public void left_turn()
     {
         if (!anim.GetBool("left_turn"))
         {
             anim.SetBool("left_move", false);
             anim.SetBool("left_turn", true);
         }
         else
         {
             if (player_currentPosition[1]-1  >=0 )
             {
                 if (board[player_currentPosition[0], player_currentPosition[1]-1, 0] == Constant.BREEZE) board[player_currentPosition[0], player_currentPosition[1]-1, 0] = Constant.PLAYER_BREEZE;
                 else if (board[player_currentPosition[0], player_currentPosition[1]-1, 0] == Constant.STENCH) board[player_currentPosition[0], player_currentPosition[1]-1, 0] = Constant.PLAYER_STENCH;
                 else if (board[player_currentPosition[0], player_currentPosition[1]-1, 0] == 0) board[player_currentPosition[0], player_currentPosition[1]-1, 0] = Constant.PLAYER;

                 if (board[player_currentPosition[0], player_currentPosition[1], 0] == Constant.PLAYER_BREEZE) board[player_currentPosition[0], player_currentPosition[1], 0] = Constant.BREEZE;
                 else if (board[player_currentPosition[0], player_currentPosition[1], 0] == Constant.PLAYER_STENCH) board[player_currentPosition[0], player_currentPosition[1], 0] = Constant.STENCH;
                 else if (board[player_currentPosition[0], player_currentPosition[1], 0] == Constant.PLAYER) board[player_currentPosition[0], player_currentPosition[1], 0] = 0;

                 source = board[player_currentPosition[0], player_currentPosition[1], 1];
                 destination = board[player_currentPosition[0], player_currentPosition[1]-1, 1];
                 player_previousPosition[0] = player_currentPosition[0];
                 player_previousPosition[1] = player_currentPosition[1];
                 player_currentPosition[1] -= 1;
                 anim.SetBool("left_turn", false);
                 anim.SetBool("left_move", true);
             }

         }
     }

     public void left_move()
     {

     }

     public void right_turn()
     {
         if (!anim.GetBool("right_turn"))
         {
             anim.SetBool("right_move", false);
             anim.SetBool("right_turn", true);
         }
         else
         {
             if (player_currentPosition[1] + 1 < 10)
             {
                 if (board[player_currentPosition[0], player_currentPosition[1] +1, 0] == Constant.BREEZE) board[player_currentPosition[0], player_currentPosition[1] + 1, 0] = Constant.PLAYER_BREEZE;
                 else if (board[player_currentPosition[0], player_currentPosition[1] + 1, 0] == Constant.STENCH) board[player_currentPosition[0], player_currentPosition[1] + 1, 0] = Constant.PLAYER_STENCH;
                 else if (board[player_currentPosition[0], player_currentPosition[1] + 1, 0] == 0) board[player_currentPosition[0], player_currentPosition[1] + 1, 0] = Constant.PLAYER;

                 if (board[player_currentPosition[0], player_currentPosition[1], 0] == Constant.PLAYER_BREEZE) board[player_currentPosition[0], player_currentPosition[1], 0] = Constant.BREEZE;
                 else if (board[player_currentPosition[0], player_currentPosition[1], 0] == Constant.PLAYER_STENCH) board[player_currentPosition[0], player_currentPosition[1], 0] = Constant.STENCH;
                 else if (board[player_currentPosition[0], player_currentPosition[1], 0] == Constant.PLAYER) board[player_currentPosition[0], player_currentPosition[1], 0] = 0;
                 source = board[player_currentPosition[0], player_currentPosition[1], 1];
                 destination = board[player_currentPosition[0], player_currentPosition[1] + 1, 1];
                 player_previousPosition[0] = player_currentPosition[0];
                 player_previousPosition[1] = player_currentPosition[1];
                 player_currentPosition[1]+=1;

                 anim.SetBool("right_turn", false);
                 anim.SetBool("right_move", true);
             }

         }

     }

     public void right_move()
     {

     }*/

    public void run()
    {
        
        if (anim.GetBool("right_move"))
        {
           
            if (source <=destination)
            {
                source += speed;
                game_player.transform.position = new Vector3(source, game_player.transform.position.y,0f);
            }
            else
            {
                anim.SetBool("right_move", false);
                movingStart = true;
            }
        }
        else if (anim.GetBool("left_move"))
        {
            if (source >= destination)
            {
                source -= speed;
                game_player.transform.position = new Vector3(source, game_player.transform.position.y, 0f);

            }
            else
            {
                anim.SetBool("left_move", false);
                movingStart = true;
            }
        }
        else if (anim.GetBool("up"))
        {
            if (source <= destination)
            {
                source += speed;
                game_player.transform.position = new Vector3(game_player.transform.position.x, source, 0f);
            }
            else
            {
                anim.SetBool("up", false);
                movingStart = true;
            }

        }
        else if (anim.GetBool("down"))
        {
            if (source >= destination)
            {
                source -= speed;
                game_player.transform.position = new Vector3(game_player.transform.position.x, source, 0f);
            }
            else
            {
                anim.SetBool("down", false);
                movingStart = true;
            }
        }
    }


}
