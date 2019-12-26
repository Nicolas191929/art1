using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ch3_ART1_Clustering
{
    public partial class Form1 : Form
    {
        ART1 art1 = new ART1();

        public Form1()
        {

            //створення форми (вікна) для виводу даних
            InitializeComponent();

            this.Text = "lab5_ART1_Shahoferov";
            this.Height = 650;
            this.Width = 1100; 
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //стовпці - назви телефонів, які доступні для "покупки"
            List<string> columns = new List<string>() {
                          "iPhone 6", "iPhone 7", "iPhone 8", "iPhone 9",  "iPhone X" };
         

            //база векторів-ознак - що купили покупці
            List<int[]> database = new List<int[]>
            {
                
                new int[] { 1,   1,   1,   1,   1}, //покупець 0
                new int[] { 1,   1,   1,   1,   1}, //покупець 1
                new int[] { 1,   1,   1,   1,   0}, //покупець 2
                new int[] { 1,   1,   0,   0,   1}, //покупець 3
                new int[] { 1,   1,   0,   1,   0}, //покупець 4
                new int[] { 1,   0,   1,   0,   0}, //покупець 5
                new int[] { 1,   0,   0,   0,   1}, //покупець 6
                new int[] { 1,   1,   1,   0,   0}, //покупець 7
                new int[] { 1,   1,   0,   1,   1}, //покупець 8
                new int[] { 1,   1,   0,   0,   0}  //покупець 9
            };

            art1.addData(database);
            
            tbResults.Text += "Кластери" + Environment.NewLine;
            tbResults.Text += "            " +  String.Join("       ", columns.ToArray()) + Environment.NewLine;
            tbResults.Text += art1.getClusters();
            tbResults.Text += Environment.NewLine;


            tbResults.Text += "Рекомендації" + Environment.NewLine;
            tbResults.Text += art1.getRecommendations();
        }
    }


    public class ART1
    {
        List<string> columns = new List<string>() {
                          "iPhone 6", "iPhone 7", "iPhone 8", "iPhone 9",  "iPhone X"};
        
        public List<FeatureVector> customers = new List<FeatureVector>();
        public List<Cluster> clusters = new List<Cluster>();
        
        double beta = 1.0; //параметр схожості
        double ro = 0.99; //параметр уважності

        public void addData(List<int[]> inputCustomers)
        {
            int customerIndex = 0;
            foreach (int[] customer in inputCustomers)
            {
                this.customers.Add(new FeatureVector(customerIndex, customer));
                customerIndex++;
            }

            createClusters();
            makeRecommendations();
        }
        private void createClusters()
        {
            //поки є зміні - буде йти цикл
            bool done = false; int limit = 50;
            while (!done)
            {
                done = true;

                //для кожного покупця пройти по кожному кластеру і перевірити чи можна туди додати цього покупця
                foreach (FeatureVector customer in this.customers)
                {
                    foreach (Cluster cluster in clusters)
                    {
                        if (cluster == customer.cluster)
                        { continue; }

                        if (ProximityTest(cluster.featuresPrototype, customer.features))
                        {
                            if (VigilenceTest(cluster.featuresPrototype, customer.features))
                            {
                                Cluster oldCluster = customer.cluster;
                                customer.cluster = cluster;

                                //перебудувати попередній кластер, якому належав покупець
                                if (oldCluster != null)
                                {
                                    List<FeatureVector> oldCustomers = customers.FindAll(c => c.cluster == oldCluster);
                                    if (oldCustomers.Count == 0) { clusters.Remove(oldCluster); }

                                    if (oldCustomers.Count > 0) oldCluster.featuresPrototype = oldCustomers[0].features;
                                    foreach (FeatureVector c in oldCustomers)
                                    {
                                        oldCluster.featuresPrototype = BitwiseAnd(oldCluster.featuresPrototype, c.features);
                                    }
                                }

                                //перебудувати новий кластер, до якого додали покупця
                                List<FeatureVector> newCustomers = customers.FindAll(c => c.cluster == cluster);
                                if (newCustomers.Count > 0) cluster.featuresPrototype = newCustomers[0].features;
                                foreach (FeatureVector c in newCustomers)
                                {
                                    cluster.featuresPrototype = BitwiseAnd(cluster.featuresPrototype, c.features);
                                }

                                //оскільки була зміна то вертаєм done на false, що продовжує цикл
                                done = false;
                                break;
                            }
                        }
                    }

                    //створити новий кластер якщо покупець не зміг приєднатись до жодного
                    if (customer.cluster == null)
                    {
                        Cluster newCluster = new Cluster(customer);
                        clusters.Add(newCluster);
                        customer.cluster = newCluster;
                        done = false;
                    }
                }
                limit--;
                if (limit == 0) break;
            }
        }
        private void makeRecommendations()
        {
            foreach (FeatureVector customer in customers)
            {
                customer.recommendation = new int[customer.features.Length];

                //цикл по всім іншим покупкцям цього кластера
                foreach(FeatureVector clusterMember in customers.FindAll(cm=> cm!= customer && cm.cluster == customer.cluster))
                {
                    for(int f =0; f< customer.features.Length; f++)
                    {
                        //якщо покупець не купив цю ознаку(гру) - просумувати скільки її купили інших покупкців
                        if (customer.features[f] == 0)
                        { customer.recommendation[f] += clusterMember.features[f]; }
                    }
                }
            }
        }

        //для виводу рядків у вікні
        public string getClusters()
        {
            string s = "";

            foreach (Cluster cluster in clusters)
            {
                s += "  Прототип:        " + itemToString(cluster.featuresPrototype, "            ") + Environment.NewLine;
                foreach (FeatureVector customer in customers.FindAll(c => c.cluster == cluster))
                {
                    s += "Покупець " + customer.index + ":        " + itemToString(customer.features, "            ") + Environment.NewLine;
                }
                s += Environment.NewLine; ;

            }

            return s;
        }
        public string getRecommendations()
        {
            string s = "";
            foreach (FeatureVector customer in customers)
            {
                s += "Покупець " + customer.index + ":        " + itemToString(customer.recommendation, "            ");

                if (customer.recommendation.Sum() != 0)
                {
                    //беремо індекс з максимальним числом інших покупкців, що купили гру, і виводимо її ім'я
                    s +=  columns[customer.recommendation.ToList().IndexOf(customer.recommendation.Max())] + Environment.NewLine;
                }
                else
                {
                    s += "-" + Environment.NewLine; ;
                }
            }

            return s;
        }
        private string itemToString(int[] item, string del)
        {
            string s = "";
            foreach (int i in item)
            {
                s += i + del;
            }

            return s;
        }

        //ТЕСТ НА СХОЖІСТЬ 
        private bool ProximityTest(int[] prototype, int[] newItem)
        {
            //Check that dimensions agree
            if (newItem.Length != prototype.Length)
            { throw new ArgumentException("Вектори не однакового розміру!"); }

            //Calculate comparison
            double left = ((double)BitwiseAnd(newItem, prototype).Sum()) / (double)(beta + prototype.Sum());
            double right = ((double)newItem.Sum()) / (beta + prototype.Length);

            //Compute comparison
            return left > right;
        }

        //ТЕСТ НА УВАЖНІСТЬ 
        private bool VigilenceTest(int[] prototype, int[] newItem)
        {
            double res = ((double)BitwiseAnd(newItem, prototype).Sum()) / newItem.Sum();

            return res < ro;
        }

        //функція для знаходження ПОБІТОВОГО AND-ВЕКТОРУ
        private int[] BitwiseAnd(int[] A, int[] B)
        {
            if (A.Length != B.Length)
            { throw new ArgumentException("Вектори не однакового розміру!"); }

            int[] result = new int[A.Length];
            for (int i = 0; i < A.Length; i++)
            {
                if (A[i] == 1 && B[i] == 1)
                { result[i] = 1; }
            }

            return result;
        }

    }
    public class Cluster
    {
        public int[] featuresPrototype;

        public Cluster(FeatureVector fv)
        {
            this.featuresPrototype = fv.features;
        }

        public string prototypeAsString
        {
            get
            {
                string s = "";
                foreach (int i in featuresPrototype)
                {
                    s += i + " ";
                }

                return s;
            }
        }
    }
    public class FeatureVector
    {
        public int index;
        public Cluster cluster = null;
        public int[] features;
        public int[] recommendation;

        public int this[int index]
        {
            get { return features[index]; }
            set { features[index] = value; }
        }

        public FeatureVector(int index, int[] features)
        {
            this.index = index;
            this.features = features;
        }

        public Cluster Cluster
        {
            get { return cluster; }
            set { cluster = value; }
        }
    }
}
