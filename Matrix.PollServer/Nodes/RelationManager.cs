using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace Matrix.PollServer.Nodes
{
    class RelationManager
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(RelationManager));

        private List<Relation> relations = new List<Relation>();

        private IDictionary<Guid, IDictionary<string, IList<Relation>>> i2o = new Dictionary<Guid, IDictionary<string, IList<Relation>>>();
        private IDictionary<Guid, IDictionary<string, IList<Relation>>> o2i = new Dictionary<Guid, IDictionary<string, IList<Relation>>>();

        public void ReSet(IEnumerable<dynamic> raws)
        {
            Cleare();
            List<Relation> newRelations = new List<Relation>();
            foreach (var rawRelation in raws)
            {
                Relation r = new Relation(rawRelation);

                newRelations.Add(r);
            }

            if (newRelations.Count == 0)
            {
                log.Info(string.Format("связи не загружены"));
                return;
            }

            lock (relations)
            {
                relations.AddRange(newRelations);
            }

            foreach (var x in newRelations.GroupBy(r => r.GetStartId()))
            {
                var dictionary = new Dictionary<string, IList<Relation>>();
                foreach (var y in x.GroupBy(t => t.GetType()))
                {
                    dictionary.Add(y.Key, y.ToList());
                }

                lock (i2o)
                {
                    i2o.Add(x.Key, dictionary);
                }
            }

            foreach (var x in newRelations.GroupBy(r => r.GetEndId()))
            {
                var dictionary = new Dictionary<string, IList<Relation>>();
                foreach (var y in x.GroupBy(t => t.GetType()))
                {
                    dictionary.Add(y.Key, y.ToList());
                }

                lock (o2i)
                {
                    o2i.Add(x.Key, dictionary);
                }
            }

            log.Info(string.Format("связи загружены, {0} шт.", relations.Count));
        }

        public void Update(dynamic msgChange)
        {
            log.Warn(string.Format("метод update еще не реализован"));
        }

        public void Cleare()
        {
            lock (relations)
            {
                relations.Clear();
            }
            lock (i2o)
            {
                i2o.Clear();
            }
            lock (o2i)
            {
                o2i.Clear();
            }
        }

        private RelationManager() { }
        static RelationManager() { }
        private static RelationManager instance = new RelationManager();
        public static RelationManager Instance
        {
            get
            {
                return instance;
            }
        }

        public IEnumerable<Relation> GetInputs(Guid id, string type)
        {
            lock (o2i)
            {
                if (!o2i.ContainsKey(id)) return new Relation[] { };

                if (!o2i[id].ContainsKey(type)) return new Relation[] { };

                return o2i[id][type];
            }
        }

        public IEnumerable<Relation> GetOutputs(Guid id, string type)
        {
            lock (i2o)
            {
                if (!i2o.ContainsKey(id)) return new Relation[] { };


                if (!i2o[id].ContainsKey(type)) return new Relation[] { };

                return i2o[id][type];
            }
        }

        public Relation Get(Guid start, Guid end)
        {
            return relations.Where(r => r.GetStartId() == start && r.GetEndId() == end).FirstOrDefault();
        }

        public void Add(Relation relation)
        {
            relations.Add(relation);
            if (i2o.ContainsKey(relation.GetStartId()))
            {
                if (i2o[relation.GetStartId()].ContainsKey(relation.GetType()))
                {
                    i2o[relation.GetStartId()][relation.GetType()].Add(relation);
                }
                else
                {
                    i2o[relation.GetStartId()].Add(new KeyValuePair<string, IList<Relation>>(relation.GetType(), new List<Relation> { relation }));
                }
            }
            else
            {
                i2o.Add(new KeyValuePair<Guid, IDictionary<string, IList<Relation>>>(relation.GetStartId(), new Dictionary<string, IList<Relation>>() { { relation.GetType(), new List<Relation> { relation } } }));
            }

            if (o2i.ContainsKey(relation.GetEndId()))
            {
                if (o2i[relation.GetEndId()].ContainsKey(relation.GetType()))
                {
                    o2i[relation.GetEndId()][relation.GetType()].Add(relation);
                }
                else
                {
                    o2i[relation.GetEndId()].Add(new KeyValuePair<string, IList<Relation>>(relation.GetType(), new List<Relation> { relation }));
                }
            }
            else
            {
                o2i.Add(new KeyValuePair<Guid, IDictionary<string, IList<Relation>>>(relation.GetEndId(), new Dictionary<string, IList<Relation>>() { { relation.GetType(), new List<Relation> { relation } } }));
            }
        }

        public void Delete(Guid start, Guid end, string type)
        {
            var relation = relations.FirstOrDefault(r => r.GetStartId() == start && r.GetEndId() == end && r.GetType() == type);
            if (relation == null) return;
            lock (relations) relations.Remove(relation);
            lock (i2o) if (i2o.ContainsKey(start) && i2o[start].ContainsKey(type)) i2o[start][type].Remove(relation);
            lock (o2i) if (o2i.ContainsKey(end) && o2i[end].ContainsKey(type)) o2i[end][type].Remove(relation);
        }
    }
}
